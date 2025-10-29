using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CCEAPI.Model.DTOs
{
    // DTOs for external API responses
    public class CountryApiResponse
    {
        public string Name { get; set; } = string.Empty;
        public string? Capital { get; set; }
        public string? Region { get; set; }
        public long Population { get; set; }
        public string? Flag { get; set; }
        public List<Currency>? Currencies { get; set; }
    }

    public class Currency
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public string? Symbol { get; set; }
    }

    public class ExchangeRateResponse
    {
        public string Result { get; set; } = string.Empty;
        public Dictionary<string, decimal> Rates { get; set; } = new();
    }

    // Response DTOs
    public class CountryResponse
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        [JsonPropertyName("capital")]
        public string? Capital { get; set; }
        
        [JsonPropertyName("region")]
        public string? Region { get; set; }
        
        [JsonPropertyName("population")]
        public long Population { get; set; }
        
        [JsonPropertyName("currency_code")]
        public string? CurrencyCode { get; set; }
        
        [JsonPropertyName("exchange_rate")]
        public decimal? ExchangeRate { get; set; }
        
        [JsonPropertyName("estimated_gdp")]
        public decimal? EstimatedGdp { get; set; }
        
        [JsonPropertyName("flag_url")]
        public string? FlagUrl { get; set; }
        
        [JsonPropertyName("last_refreshed_at")]
        public DateTime LastRefreshedAt { get; set; }
    }

    public class StatusResponse
    {
        [JsonPropertyName("total_countries")]
        public int TotalCountries { get; set; }
        
        [JsonPropertyName("last_refreshed_at")]
        public DateTime? LastRefreshedAt { get; set; }
    }

    public class ErrorResponse
    {
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;
        
        [JsonPropertyName("details")]
        public object? Details { get; set; }
    }
}