using CCEAPI.Model;
using CCEAPI.Data;
using Microsoft.EntityFrameworkCore;
using CCEAPI.Services;
using System.Text.Json;

namespace CCEAPI.Services 
{
    public class CountryService 
    {
        private readonly AddDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IImageService _imageservice;

        public CountryService(AppDbContext context, HttpClient httpClient, ImageService imageservice) 
        {
            _context = context;
            _httpClient = httpClient;
            _imageservice = imageservice;
        }
    }

    

    public async Task<List<Country>> FetchCountriesFromApiAsync()
    {
        string apiUrl = "https://restcountries.com/v2/all?fields=name,capital,region,population,flag,currencies";

        try
        {
            var response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch data from REST Countries API.");
            }

            var json = await response.Content.ReadAsStringAsync();

            // Deserialize into dynamic objects
            var countriesData = System.Text.Json.JsonSerializer.Deserialize<List<dynamic>>(json);

            var countries = new List<Country>();

            if (countriesData != null)
            {
                foreach (var item in countriesData)
                {
                    string? currencyCode = null;

                    try
                    {
                        // Some countries have multiple currencies, take the first one
                        if (item?.GetProperty("currencies").ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            var firstCurrency = item?.GetProperty("currencies")[0];
                            if (firstCurrency.TryGetProperty("code", out var codeProp))
                            {
                                currencyCode = codeProp.GetString();
                            }
                        }
                    }
                    catch
                    {
                        // ignore if structure is missing
                    }

                    countries.Add(new Country
                    {
                        Id = Guid.NewGuid(),
                        Name = item.GetProperty("name").GetString(),
                        Capital = item.TryGetProperty("capital", out var cap) ? cap.GetString() : null,
                        Region = item.TryGetProperty("region", out var reg) ? reg.GetString() : null,
                        Population = item.TryGetProperty("population", out var pop) ? pop.GetInt64() : 0,
                        CurrencyCode = currencyCode,
                        FlagUrl = item.TryGetProperty("flag", out var flag) ? flag.GetString() : null,
                        ExchangeRate = 0,
                        EstimateGdp = 0,
                        LastRefreshedAt = DateTime.UtcNow
                    });
                }
            }

            return countries;
        }
        catch (Exception ex)
        {
            HandleExternalApiError("REST Countries API", ex);
            return new List<Country>();
        }
    }
    public async Task<Dictionary<string, decimal>> FetchExchangeRatesAsync()
    {
        string apiUrl = "https://open.er-api.com/v6/latest/USD";

        try
        {
            var response = await _httpClient.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Failed to fetch data from Exchange Rate API.");
            }

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("rates", out JsonElement ratesElement))
            {
                throw new Exception("Rates data not found in response.");
            }

            var exchangeRates = new Dictionary<string, decimal>();

            foreach (var rate in ratesElement.EnumerateObject())
            {
                if (rate.Value.TryGetDecimal(out decimal value))
                {
                    exchangeRates[rate.Name] = value;
                }
            }

            return exchangeRates;
        }
        catch (Exception ex)
        {
            HandleExternalApiError("Exchange Rate API", ex);
            return new Dictionary<string, decimal>();
        }
    }
    private decimal ComputeEstimatedGdp(long population, decimal exchangeRate)
    {
        try
        {
            // Simple estimation formula
            return population * exchangeRate * 0.001m;
        }
        catch
        {
            return 0;
        }
    }
    public async Task RefreshCountriesAsync()
    {
        var countryApi = "https://restcountries.com/v2/all?fields=name,capital,region,population,flag,currencies";
        var exchangeRateApi = "https://open.er-api.com/v6/latest/USD";

        // 1️⃣ Fetch countries data
        var countryResponse = await _httpClient.GetAsync(countryApi);
        if (!countryResponse.IsSuccessStatusCode)
            throw new Exception("Failed to fetch countries from REST Countries API");

        var countryJson = await countryResponse.Content.ReadAsStringAsync();
        var countriesData = JsonSerializer.Deserialize<List<JsonElement>>(countryJson);

        // 2️⃣ Fetch exchange rates
        var rateResponse = await _httpClient.GetAsync(exchangeRateApi);
        if (!rateResponse.IsSuccessStatusCode)
            throw new Exception("Failed to fetch data from Exchange Rate API");

        var rateJson = await rateResponse.Content.ReadAsStringAsync();
        var rateData = JsonSerializer.Deserialize<JsonElement>(rateJson);
        var rates = rateData.GetProperty("rates");

        // 3️⃣ Loop through countries and prepare data
        foreach (var item in countriesData)
        {
            string name = item.GetProperty("name").GetString() ?? "";
            string capital = item.TryGetProperty("capital", out var cap) ? cap.GetString() ?? "" : "";
            string region = item.TryGetProperty("region", out var reg) ? reg.GetString() ?? "" : "";
            long population = item.TryGetProperty("population", out var pop) ? pop.GetInt64() : 0;
            string flag = item.TryGetProperty("flag", out var flg) ? flg.GetString() ?? "" : "";

            // Handle currency
            string? currencyCode = null;
            decimal? exchangeRate = null;
            decimal estimatedGdp = 0;

            if (item.TryGetProperty("currencies", out var currencies) && currencies.ValueKind == JsonValueKind.Array && currencies.GetArrayLength() > 0)
            {
                currencyCode = currencies[0].GetProperty("code").GetString();

                if (currencyCode != null && rates.TryGetProperty(currencyCode, out var rateElement))
                {
                    exchangeRate = rateElement.GetDecimal();
                    estimatedGdp = ComputeEstimatedGdp(population, exchangeRate.Value);
                }
            }

            // 4️⃣ Check if the country already exists
            var existing = await _context.Countries.FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

            if (existing != null)
            {
                // Update
                existing.Capital = capital;
                existing.Region = region;
                existing.Population = population;
                existing.CurrencyCode = currencyCode;
                existing.ExchangeRate = exchangeRate ?? 0;
                existing.EstimateGdp = estimatedGdp;
                existing.FlagUrl = flag;
                existing.LastRefreshedAt = DateTime.UtcNow;
            }
            else
            {
                // Insert
                var country = new Country
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Capital = capital,
                    Region = region,
                    Population = population,
                    CurrencyCode = currencyCode,
                    ExchangeRate = exchangeRate ?? 0,
                    EstimateGdp = estimatedGdp,
                    FlagUrl = flag,
                    LastRefreshedAt = DateTime.UtcNow
                };

                await _context.Countries.AddAsync(country);
            }
        }
        // 5️ Save all changes
        await _context.SaveChangesAsync();

        var allCountries = await _context.Countries.ToListAsync();
        await _imageService.GenerateSummaryImageAsync(allCountries);
    }

    public async Task<IEnumerable<Country>> GetAllCountriesAsync(string? region = null, string? currency = null, string? sort = null)
    {
        var query = _context.Countries.AsQueryable();

        // Filter by region
        if (!string.IsNullOrEmpty(region))
            query = query.Where(c => c.Region.ToLower() == region.ToLower());

        // Filter by currency
        if (!string.IsNullOrEmpty(currency))
            query = query.Where(c => c.CurrencyCode.ToLower() == currency.ToLower());

        // Sorting
        if (!string.IsNullOrEmpty(sort))
        {
            switch (sort.ToLower())
            {
                case "gdp_desc":
                    query = query.OrderByDescending(c => c.EstimateGdp);
                    break;
                case "gdp_asc":
                    query = query.OrderBy(c => c.EstimateGdp);
                    break;
                case "population_desc":
                    query = query.OrderByDescending(c => c.Population);
                    break;
                case "population_asc":
                    query = query.OrderBy(c => c.Population);
                    break;
            }
        }

        return await query.ToListAsync();
    }
    public async Task<object> GetStatusAsync()
    {
        var totalCountries = await _context.Countries.CountAsync();
        var lastRefresh = await _context.Countries
            .OrderByDescending(c => c.LastRefreshedAt)
            .Select(c => c.LastRefreshedAt)
            .FirstOrDefaultAsync();

        return new
        {
            total_countries = totalCountries,
            last_refreshed_at = lastRefresh
        };
    }
    public async Task<Country?> GetCountryByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Country name is required.");

        var country = await _context.Countries
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

        return country;
    }
    public async Task<bool> DeleteCountryAsync(string name)
    {
        var country = await _context.Countries
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());

        if (country == null)
            return false;

        _context.Countries.Remove(country);
        await _context.SaveChangesAsync();

        return true;
    }
    private void HandleExternalApiError(string source, Exception ex)
    {
        Console.WriteLine($"[{source}] Error: {ex.Message}");
    }

}