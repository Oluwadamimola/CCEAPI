namespace CCEAPI.Model 
{
    public class Country 
    {
        public Guid Id { get ; set; }
        public string? Name { get ; set; }
        public string? Capital { get ; set; }
        public string? Region { get ; set; }
        public long Population { get ; set; }
        public string? CurrencyCode { get ; set; }
        public decimal ExchangeRate { get ; set; }
        public decimal EstimateGdp { get ; set; }
        public string? FlagUrl { get ; set; }
        public DateTime? LastRefreshedAt { get ; set; }
    }
}