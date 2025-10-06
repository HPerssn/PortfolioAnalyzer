using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioAnalyzer.Core.Models
{
    public class Asset
    {
        public string Symbol { get; set; }
        public decimal Quantity { get; set; }
        public decimal AverageCost { get; set; }
        public decimal CurrentPrice { get; set; }
        public DateTime PurchaseDate { get; set; }
        public List<decimal> HistoricalPrices { get; set; }

        // Constructor for the console application usage
        public Asset(string symbol, DateTime purchaseDate)
        {
            Symbol = symbol;
            PurchaseDate = purchaseDate;
            HistoricalPrices = new List<decimal>();
            Quantity = 0;
            AverageCost = 0;
            CurrentPrice = 0;
        }

        // Constructor for simple usage
        public Asset(string symbol, decimal quantity, decimal averageCost)
        {
            Symbol = symbol;
            Quantity = quantity;
            AverageCost = averageCost;
            CurrentPrice = 0;
            PurchaseDate = DateTime.Now;
            HistoricalPrices = new List<decimal>();
        }

        public decimal GetTotalCost()
        {
            return Quantity * AverageCost;
        }

        public decimal GetCurrentValue()
        {
            return Quantity * CurrentPrice;
        }

        public decimal GetReturn()
        {
            return GetCurrentValue() - GetTotalCost();
        }

        public decimal GetReturnPercentage()
        {
            var totalCost = GetTotalCost();
            return totalCost > 0 ? (GetReturn() / totalCost) * 100 : 0;
        }
    }
}
