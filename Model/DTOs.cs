using System;
using System.Collections.Generic;

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
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Capital { get; set; }
        public string? Region { get; set; }
        public long Population { get; set; }
        public string? CurrencyCode { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? EstimatedGdp { get; set; }
        public string? FlagUrl { get; set; }
        public DateTime LastRefreshedAt { get; set; }
    }

    public class StatusResponse
    {
        public int TotalCountries { get; set; }
        public DateTime? LastRefreshedAt { get; set; }
    }

    public class ErrorResponse
    {
        public string Error { get; set; } = string.Empty;
        public object? Details { get; set; }
    }
}