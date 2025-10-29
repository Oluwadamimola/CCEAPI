using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using CCEAPI.Data;
using CCEAPI.Model;
using CCEAPI.Model.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CCEAPI.Services
{
    public class CountryService
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IImageService _imageService;
        private readonly Random _random = new Random();

        public CountryService(AppDbContext context, IImageService imageService)
        {
            _context = context;
            _imageService = imageService;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task RefreshCountriesAsync()
        {
            try
            {
                Console.WriteLine("=== STARTING REFRESH ===");
                
                // Fetch countries data
                var countriesResponse = await _httpClient.GetStringAsync(
                    "https://restcountries.com/v2/all?fields=name,capital,region,population,flag,currencies");
                
                Console.WriteLine($"Got response length: {countriesResponse.Length}");
                Console.WriteLine($"First 300 chars: {countriesResponse.Substring(0, Math.Min(300, countriesResponse.Length))}");
                
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var countries = JsonSerializer.Deserialize<List<CountryApiResponse>>(countriesResponse, options);

                Console.WriteLine($"Deserialized countries count: {countries?.Count ?? 0}");

                if (countries == null || !countries.Any())
                {
                    throw new Exception("No countries data received from restcountries API");
                }

                // Fetch exchange rates
                var ratesResponse = await _httpClient.GetStringAsync("https://open.er-api.com/v6/latest/USD");
                var exchangeRates = JsonSerializer.Deserialize<ExchangeRateResponse>(ratesResponse, options);

                Console.WriteLine($"Exchange rates count: {exchangeRates?.Rates?.Count ?? 0}");

                if (exchangeRates == null || exchangeRates.Rates == null)
                {
                    throw new Exception("No exchange rates data received from exchange rate API");
                }

                var now = DateTime.UtcNow;
                int addedCount = 0;

                foreach (var countryData in countries)
                {
                    if (string.IsNullOrEmpty(countryData.Name))
                    {
                        Console.WriteLine("Skipping country with no name");
                        continue;
                    }

                    string? currencyCode = countryData.Currencies?.FirstOrDefault()?.Code;
                    decimal? exchangeRate = null;
                    decimal? estimatedGdp = null;

                    if (!string.IsNullOrEmpty(currencyCode))
                    {
                        if (exchangeRates.Rates.TryGetValue(currencyCode, out var rate))
                        {
                            exchangeRate = rate;
                            var randomMultiplier = _random.Next(1000, 2001);
                            estimatedGdp = (countryData.Population * randomMultiplier) / rate;
                        }
                    }
                    else
                    {
                        estimatedGdp = 0;
                    }

                    var existingCountry = await _context.Countries
                        .FirstOrDefaultAsync(c => c.Name != null && c.Name.ToLower() == countryData.Name.ToLower());

                    if (existingCountry != null)
                    {
                        existingCountry.Capital = countryData.Capital;
                        existingCountry.Region = countryData.Region;
                        existingCountry.Population = countryData.Population;
                        existingCountry.CurrencyCode = currencyCode;
                        existingCountry.ExchangeRate = exchangeRate;
                        existingCountry.EstimatedGdp = estimatedGdp;
                        existingCountry.FlagUrl = countryData.Flag;
                        existingCountry.LastRefreshedAt = now;
                    }
                    else
                    {
                        var newCountry = new Country
                        {
                            Id = Guid.NewGuid(),
                            Name = countryData.Name,
                            Capital = countryData.Capital,
                            Region = countryData.Region,
                            Population = countryData.Population,
                            CurrencyCode = currencyCode,
                            ExchangeRate = exchangeRate,
                            EstimatedGdp = estimatedGdp,
                            FlagUrl = countryData.Flag,
                            LastRefreshedAt = now
                        };
                        await _context.Countries.AddAsync(newCountry);
                        addedCount++;
                    }
                }

                Console.WriteLine($"About to save {addedCount} countries to database...");
                await _context.SaveChangesAsync();
                Console.WriteLine("SAVED TO DATABASE!");

                var metadata = await _context.RefreshMetadata.FirstOrDefaultAsync();
                var totalCountries = await _context.Countries.CountAsync();

                Console.WriteLine($"Total countries in DB: {totalCountries}");

                if (metadata == null)
                {
                    metadata = new RefreshMetadata
                    {
                        LastRefreshedAt = now,
                        TotalCountries = totalCountries
                    };
                    await _context.RefreshMetadata.AddAsync(metadata);
                }
                else
                {
                    metadata.LastRefreshedAt = now;
                    metadata.TotalCountries = totalCountries;
                }

                await _context.SaveChangesAsync();

                await _imageService.GenerateSummaryImageAsync();
                Console.WriteLine("=== REFRESH COMPLETE ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                Console.WriteLine($"Stack: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<List<CountryResponse>> GetCountriesAsync(string? region, string? currency, string? sort)
        {
            var query = _context.Countries.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(region))
            {
                query = query.Where(c => c.Region != null && c.Region.ToLower() == region.ToLower());
            }

            if (!string.IsNullOrEmpty(currency))
            {
                query = query.Where(c => c.CurrencyCode != null && c.CurrencyCode.ToUpper() == currency.ToUpper());
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(sort))
            {
                query = sort.ToLower() switch
                {
                    "gdp_desc" => query.OrderByDescending(c => c.EstimatedGdp ?? 0),
                    "gdp_asc" => query.OrderBy(c => c.EstimatedGdp ?? 0),
                    "name_asc" => query.OrderBy(c => c.Name),
                    "name_desc" => query.OrderByDescending(c => c.Name),
                    "population_desc" => query.OrderByDescending(c => c.Population),
                    "population_asc" => query.OrderBy(c => c.Population),
                    _ => query
                };
            }

            var countries = await query.ToListAsync();

            return countries.Select(c => new CountryResponse
            {
                Id = c.Id,
                Name = c.Name ?? string.Empty,
                Capital = c.Capital,
                Region = c.Region,
                Population = c.Population,
                CurrencyCode = c.CurrencyCode,
                ExchangeRate = c.ExchangeRate,
                EstimatedGdp = c.EstimatedGdp,
                FlagUrl = c.FlagUrl,
                LastRefreshedAt = c.LastRefreshedAt
            }).ToList();
        }

        public async Task<CountryResponse?> GetCountryByNameAsync(string name)
        {
            var country = await _context.Countries
                .FirstOrDefaultAsync(c => c.Name != null && c.Name.ToLower() == name.ToLower());

            if (country == null) return null;

            return new CountryResponse
            {
                Id = country.Id,
                Name = country.Name ?? string.Empty,
                Capital = country.Capital,
                Region = country.Region,
                Population = country.Population,
                CurrencyCode = country.CurrencyCode,
                ExchangeRate = country.ExchangeRate,
                EstimatedGdp = country.EstimatedGdp,
                FlagUrl = country.FlagUrl,
                LastRefreshedAt = country.LastRefreshedAt
            };
        }

        public async Task<bool> DeleteCountryAsync(string name)
        {
            var country = await _context.Countries
                .FirstOrDefaultAsync(c => c.Name != null && c.Name.ToLower() == name.ToLower());

            if (country == null) return false;

            _context.Countries.Remove(country);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<StatusResponse> GetStatusAsync()
        {
            var metadata = await _context.RefreshMetadata.FirstOrDefaultAsync();
            var totalCountries = await _context.Countries.CountAsync();

            return new StatusResponse
            {
                TotalCountries = totalCountries,
                LastRefreshedAt = metadata != null ? metadata.LastRefreshedAt : null
            };
        }
    }
}