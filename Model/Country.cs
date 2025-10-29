using System;
using System.ComponentModel.DataAnnotations;
namespace CCEAPI.Model
{
    public class Country
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Name is required")]
        public string? Name { get; set; }
        public string? Capital { get; set; }
        public string? Region { get; set; }

        [Required(ErrorMessage = "Population is required")]
        public long Population { get; set; }

        public string? CurrencyCode { get; set; }
        public decimal? ExchangeRate { get; set; }
        public decimal? EstimatedGdp { get; set; }
        public string? FlagUrl { get; set; }
        public DateTime LastRefreshedAt { get; set; } = DateTime.UtcNow;
    }

}