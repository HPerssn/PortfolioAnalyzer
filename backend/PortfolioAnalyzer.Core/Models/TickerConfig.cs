using System;
using System.Collections.Generic;

namespace PortfolioAnalyzer.Core.Models
{
    public class TickerConfig
    {
        public string Symbol { get; set; } = "";
        public decimal Quantity { get; set; }
        public decimal AverageCost { get; set; }
        public decimal PurchasePrice { get; set; }
        public DateTime PurchaseDate { get; set; }
        public List<decimal> HistoricalPrices { get; set; } = new List<decimal>();
        public string? Notes { get; set; }
    }
}