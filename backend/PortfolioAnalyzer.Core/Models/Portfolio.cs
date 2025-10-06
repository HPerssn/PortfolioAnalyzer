using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioAnalyzer.Core.Models
{
    public class Portfolio
    {
        public List<Asset> Assets { get; private set; }
        
        public Portfolio()
        {
            Assets = new List<Asset>();
        }

        public void AddAsset(Asset asset)
        {
            Assets.Add(asset);
        }

        public decimal GetTotalValue()
        {
            return Assets.Sum(a => a.GetCurrentValue());
        }

        public decimal GetTotalCost()
        {
            return Assets.Sum(a => a.GetTotalCost());
        }

        public decimal GetTotalReturn()
        {
            return Assets.Sum(a => a.GetReturn());
        }

        public decimal GetTotalReturnPercentage()
        {
            var totalCost = GetTotalCost();
            return totalCost > 0 ? (GetTotalReturn() / totalCost) * 100 : 0;
        }
    }
}
