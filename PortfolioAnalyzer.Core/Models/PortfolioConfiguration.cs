using System;
using System.Collections.Generic;

namespace PortfolioAnalyzer.Core.Models
{
    public class PortfolioConfiguration
    {
        public string Name { get; set; } = "My Portfolio";
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public List<string> Symbols { get; set; } = new List<string>();
        public List<TickerConfig> Tickers { get; set; } = new List<TickerConfig>();
        public decimal CashBalance { get; set; } = 0;
        public string? Description { get; set; }
    }
}
