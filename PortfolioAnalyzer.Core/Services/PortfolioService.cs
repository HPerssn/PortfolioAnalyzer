using System;
using System.Collections.Generic;
using System.Linq;
using PortfolioAnalyzer.Core.Models;

namespace PortfolioAnalyzer.Core.Services
{
    public class PortfolioService
    {
        private readonly Portfolio _portfolio;

        public PortfolioService(Portfolio portfolio)
        {
            _portfolio = portfolio;
        }

        public void PrintPortfolioSummary()
        {
            Console.WriteLine("\n--- Portfolio Summary ---");
            Console.WriteLine($"Total Assets: {_portfolio.Assets.Count}");
            
            foreach (var asset in _portfolio.Assets)
            {
                var effectiveShares = asset.Shares > 0 ? asset.Shares : asset.Quantity;
                Console.WriteLine($"{asset.Symbol}: {effectiveShares} shares @ ${asset.CurrentPrice:F2} = ${asset.GetCurrentValue():F2}");
            }
            
            Console.WriteLine($"\nTotal Portfolio Value: ${_portfolio.GetTotalValue():F2}");
            Console.WriteLine($"Total Cost: ${_portfolio.GetTotalCost():F2}");
            Console.WriteLine($"Total Return: ${_portfolio.GetTotalReturn():F2} ({_portfolio.GetTotalReturnPercentage():F2}%)");
        }

        public void PrintPerformanceMetrics()
        {
            Console.WriteLine("\n--- Performance Metrics ---");
            
            var totalReturn = _portfolio.GetTotalReturn();
            var totalCost = _portfolio.GetTotalCost();
            var totalValue = _portfolio.GetTotalValue();
            
            Console.WriteLine($"Total Investment: ${totalCost:F2}");
            Console.WriteLine($"Current Value: ${totalValue:F2}");
            Console.WriteLine($"Absolute Return: ${totalReturn:F2}");
            Console.WriteLine($"Percentage Return: {_portfolio.GetTotalReturnPercentage():F2}%");
            
            if (_portfolio.Assets.Any())
            {
                Console.WriteLine("\nIndividual Asset Performance:");
                foreach (var asset in _portfolio.Assets)
                {
                    var effectiveShares = asset.Shares > 0 ? asset.Shares : asset.Quantity;
                    Console.WriteLine($"  {asset.Symbol}: {asset.GetReturnPercentage():F2}% return (${asset.GetReturn():F2})");
                }
            }
        }

        public decimal CalculateTotalReturn()
        {
            return _portfolio.GetTotalReturn();
        }
    }
}
