using System;

namespace CCEAPI.Model
{
    public class RefreshMetadata
    {
        public int Id { get; set; } = 1; 
        public DateTime LastRefreshedAt { get; set; }
        public int TotalCountries { get; set; }
    }
}