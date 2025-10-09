using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using PortfolioAnalyzer.Core.Data;

namespace PortfolioAnalyzer.Api.Services
{
    public class RealDataService
    {
        public async Task<List<PriceRecord>> GetStockPricesAsync(string ticker, DateTime startDate)
        {
            return await ReadData.FetchPricesFromYahooFinanceAsync(ticker, startDate);
        }

        public async Task<decimal> GetCurrentPriceAsync(string ticker)
        {
            try
            {
                // Get the last 5 days of data to ensure we get the most recent price
                var prices = await GetStockPricesAsync(ticker, DateTime.Now.AddDays(-5));
                return prices.Count > 0 ? prices[^1].Close : 0;
            }
            catch (Exception)
            {
                return 0; // Return 0 if we can't fetch the price
            }
        }
    }
}
