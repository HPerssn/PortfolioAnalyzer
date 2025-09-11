using System;
using System.Collections.Generic;

public class TickerConfig
{
    public string Symbol { get; set; } = "";
    public decimal Quantity { get; set; }
    public List<decimal> HistoricalPrices { get; set; } = new List<decimal>();
    public DateTime PurchaseDate { get; set; }
    public decimal PurchasePrice { get; set; }
    public string? Notes { get; set; }
}

public class PortfolioConfiguration
{
    public string Name { get; set; } = "My Portfolio";
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    public List<TickerConfig> Tickers { get; set; } = new List<TickerConfig>();
    public decimal CashBalance { get; set; } = 0;
    public string? Description { get; set; }
}