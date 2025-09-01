using System;
using System.Collections.Generic;
using System.Linq;

public class Portfolio
{
    public List<Asset> Assets { get; set; } = new List<Asset>();
    public decimal Cash { get; set; } = 0;

    public decimal TotalValue => Cash + Assets.Sum(a => a.Value);

    public void AddAsset(Asset asset)
    {
        Assets.Add(asset);
    }

    public decimal CalculateInitialInvestment()
    {
        decimal total = 0;

        foreach (var asset in Assets)
        {
            // Take the first price on or after purchase date
            decimal purchasePrice = asset.HistoricalPrices.FirstOrDefault();
            total += purchasePrice * asset.Quantity;
        }

        return total;
    }

    public decimal CalculateTotalReturn()
    {
        return TotalValue - CalculateInitialInvestment();
    }

    public void PrintSummary()
    {
        Console.WriteLine("Portfolio Summary:");
        Console.WriteLine($"Cash: {Cash:C}");
        Console.WriteLine($"Total Value: {TotalValue:C}");
        Console.WriteLine($"Initial Investment: {CalculateInitialInvestment():C}");
        Console.WriteLine($"Total Return: {CalculateTotalReturn():C}");
        Console.WriteLine("Assets:");

        foreach (var asset in Assets)
        {
            Console.WriteLine($"- {asset.Symbol}: {asset.Quantity} shares at {asset.CurrentPrice:C} each, Total Value: {asset.Value:C}");
        }
    }
}
