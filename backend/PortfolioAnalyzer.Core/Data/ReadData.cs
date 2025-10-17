using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace PortfolioAnalyzer.Core.Data
{
    public class PriceRecord
    {
        [JsonPropertyName("Date")]
        public required string Date { get; set; }

        [JsonPropertyName("Close")]
        public decimal Close { get; set; }
    }

    public static class ReadData
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly MemoryCache cache = new MemoryCache(new MemoryCacheOptions());

        static ReadData()
        {
            // Add User-Agent to avoid rate limiting from Yahoo Finance
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        /// <summary>
        /// Fetches exchange rate from USD to target currency using Yahoo Finance
        /// Uses 24-hour cache as exchange rates don't change frequently
        /// </summary>
        private static async Task<decimal> GetExchangeRateAsync(string fromCurrency, string toCurrency = "USD")
        {
            if (fromCurrency == toCurrency)
                return 1.0m;

            var cacheKey = $"exchange_{fromCurrency}_{toCurrency}";

            // Check cache first (24 hour cache for exchange rates)
            if (cache.TryGetValue(cacheKey, out decimal cachedRate))
            {
                return cachedRate;
            }

            try
            {
                // Yahoo Finance forex pair format: SEKUSD=X for SEK to USD
                var forexPair = $"{fromCurrency}{toCurrency}=X";
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{forexPair}?interval=1d&range=1d";

                var response = await httpClient.GetStringAsync(url);
                var jsonDoc = JsonDocument.Parse(response);

                var result = jsonDoc.RootElement.GetProperty("chart").GetProperty("result")[0];
                var quote = result.GetProperty("meta").GetProperty("regularMarketPrice").GetDecimal();

                // Cache for 24 hours
                cache.Set(cacheKey, quote, TimeSpan.FromHours(24));

                return quote;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to fetch exchange rate for {fromCurrency} to {toCurrency}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Fetches stock prices directly from Yahoo Finance HTTP API (no Python required)
        /// Uses 30-minute cache to reduce API calls and improve performance
        /// Automatically converts prices to USD if stock is traded in different currency
        /// </summary>
        public static async Task<List<PriceRecord>> FetchPricesFromYahooFinanceAsyn(string ticker, DateTime purchaseDate)
        {
            // Create cache key based on ticker and purchase date
            var cacheKey = $"prices_{ticker}_{purchaseDate:yyyy-MM-dd}";

            // Check cache first
            if (cache.TryGetValue(cacheKey, out List<PriceRecord>? cachedPrices) && cachedPrices != null)
            {
                return cachedPrices;
            }

            try
            {
                // Convert dates to Unix timestamps
                var startTimestamp = new DateTimeOffset(purchaseDate).ToUnixTimeSeconds();
                var endTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Yahoo Finance API endpoint
                var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?period1={startTimestamp}&period2={endTimestamp}&interval=1d";

                var response = await httpClient.GetStringAsync(url);
                var jsonDoc = JsonDocument.Parse(response);

                var result = jsonDoc.RootElement.GetProperty("chart").GetProperty("result")[0];

                // Extract currency from metadata
                var currency = result.GetProperty("meta").GetProperty("currency").GetString() ?? "USD";

                var timestamps = result.GetProperty("timestamp").EnumerateArray().Select(t => t.GetInt64()).ToArray();
                var closes = result.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close").EnumerateArray().Select(c => c.GetDecimal()).ToArray();

                // Get exchange rate if needed (convert to USD)
                var exchangeRate = await GetExchangeRateAsync(currency, "USD");

                var prices = new List<PriceRecord>();
                for (int i = 0; i < timestamps.Length; i++)
                {
                    var date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).UtcDateTime;
                    prices.Add(new PriceRecord
                    {
                        Date = date.ToString("yyyy-MM-dd"),
                        Close = closes[i] * exchangeRate  // Convert to USD
                    });
                }

                var filteredPrices = prices.Where(p => DateTime.Parse(p.Date) >= purchaseDate).ToList();

                // Cache for 30 minutes
                cache.Set(cacheKey, filteredPrices, TimeSpan.FromMinutes(30));

                return filteredPrices;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to fetch prices for {ticker}: {ex.Message}", ex);
            }
        }
        public static async Task<List<PriceRecord>> FetchPricesFromYahooFinanceAsync(string ticker, DateTime purchaseDate)
        {
            var cacheKey = $"prices_{ticker}_{purchaseDate:yyyy-MM-dd}";

            // Check cache first
            if (cache.TryGetValue(cacheKey, out List<PriceRecord>? cachedPrices) && cachedPrices != null)
            {
                return cachedPrices;
            }

            try
            {
                var endDate = DateTime.UtcNow;
                var daysSpan = (endDate - purchaseDate).Days;

                // For large date ranges (>730 days / 2 years), fetch in chunks to avoid API limits
                if (daysSpan > 730)
                {
                    return await FetchPricesInChunksAsync(ticker, purchaseDate, endDate, cacheKey);
                }
                else
                {
                    // For smaller ranges, fetch in one go
                    return await FetchPricesSingleRequestAsync(ticker, purchaseDate, endDate, cacheKey);
                }
            }
            catch (Exception ex)
            {
                // Rework exception to clearly show what failed
                throw new InvalidOperationException($"Failed to fetch prices for {ticker}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Fetches prices from a single date range with retry logic (recommended for <2 years)
        /// </summary>
        private static async Task<List<PriceRecord>> FetchPricesSingleRequestAsync(string ticker, DateTime startDate, DateTime endDate, string cacheKey)
        {
            const int maxRetries = 3;
            int attemptCount = 0;

            while (attemptCount < maxRetries)
            {
                try
                {
                    var startTimestamp = new DateTimeOffset(startDate).ToUnixTimeSeconds();
                    var endTimestamp = new DateTimeOffset(endDate).ToUnixTimeSeconds();
                    var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?period1={startTimestamp}&period2={endTimestamp}&interval=1d";

                    var response = await httpClient.GetStringAsync(url);
                    var jsonDoc = JsonDocument.Parse(response);

                    // Check for the error object
                    var chartElement = jsonDoc.RootElement.GetProperty("chart");
                    if (chartElement.GetProperty("result").ValueKind == JsonValueKind.Null ||
                        chartElement.GetProperty("result").GetArrayLength() == 0)
                    {
                        var errorElement = chartElement.GetProperty("error");
                        if (errorElement.ValueKind != JsonValueKind.Null && errorElement.TryGetProperty("description", out var desc))
                        {
                            throw new InvalidOperationException($"Yahoo Finance API error for {ticker}: {desc.GetString()}");
                        }
                        throw new InvalidOperationException($"No data found for ticker {ticker}.");
                    }

                    var result = chartElement.GetProperty("result")[0];
                    var currency = result.GetProperty("meta").GetProperty("currency").GetString() ?? "USD";

                    var timestampElements = result.GetProperty("timestamp").EnumerateArray().ToArray();
                    var closeElements = result.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close").EnumerateArray().ToArray();

                    // Get exchange rate if needed
                    var exchangeRate = await GetExchangeRateAsync(currency, "USD");

                    var prices = new List<PriceRecord>();
                    for (int i = 0; i < timestampElements.Length; i++)
                    {
                        var timestampElement = timestampElements[i];
                        var closeElement = closeElements[i];

                        if (timestampElement.ValueKind == JsonValueKind.Number && closeElement.ValueKind == JsonValueKind.Number)
                        {
                            var date = DateTimeOffset.FromUnixTimeSeconds(timestampElement.GetInt64()).UtcDateTime;
                            prices.Add(new PriceRecord
                            {
                                Date = date.ToString("yyyy-MM-dd"),
                                Close = closeElement.GetDecimal() * exchangeRate
                            });
                        }
                    }

                    var filteredPrices = prices.Where(p => DateTime.Parse(p.Date) >= startDate).ToList();

                    // Validate we got reasonable amount of data (at least 50% of expected)
                    int expectedDataPoints = (int)((endDate - startDate).TotalDays * 0.7); // 70% due to weekends/holidays
                    if (filteredPrices.Count >= expectedDataPoints * 0.5)
                    {
                        // Cache for 30 minutes
                        cache.Set(cacheKey, filteredPrices, TimeSpan.FromMinutes(30));
                        return filteredPrices;
                    }
                    else
                    {
                        // Not enough data, retry
                        attemptCount++;
                        if (attemptCount < maxRetries)
                        {
                            await Task.Delay(1000 * attemptCount); // Exponential backoff
                        }
                    }
                }
                catch (HttpRequestException) when (attemptCount < maxRetries - 1)
                {
                    // Network error, retry
                    attemptCount++;
                    await Task.Delay(1000 * attemptCount);
                }
            }

            throw new InvalidOperationException($"Failed to fetch sufficient price data for {ticker} after {maxRetries} attempts.");
        }

        /// <summary>
        /// Fetches prices for large date ranges (>2 years) by splitting into 1-year chunks
        /// This circumvents Yahoo Finance API limitations for requesting large amounts of historical data
        /// </summary>
        private static async Task<List<PriceRecord>> FetchPricesInChunksAsync(string ticker, DateTime startDate, DateTime endDate, string cacheKey)
        {
            var allPrices = new List<PriceRecord>();
            var currentStart = startDate;

            // Fetch in 1-year chunks
            while (currentStart < endDate)
            {
                var chunkEnd = currentStart.AddYears(1);
                if (chunkEnd > endDate)
                    chunkEnd = endDate;

                try
                {
                    var chunkStartTimestamp = new DateTimeOffset(currentStart).ToUnixTimeSeconds();
                    var chunkEndTimestamp = new DateTimeOffset(chunkEnd).ToUnixTimeSeconds();
                    var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?period1={chunkStartTimestamp}&period2={chunkEndTimestamp}&interval=1d";

                    var response = await httpClient.GetStringAsync(url);
                    var jsonDoc = JsonDocument.Parse(response);

                    var chartElement = jsonDoc.RootElement.GetProperty("chart");
                    if (chartElement.GetProperty("result").ValueKind == JsonValueKind.Null ||
                        chartElement.GetProperty("result").GetArrayLength() == 0)
                    {
                        // Skip this chunk and move to next
                        currentStart = chunkEnd;
                        continue;
                    }

                    var result = chartElement.GetProperty("result")[0];
                    var currency = result.GetProperty("meta").GetProperty("currency").GetString() ?? "USD";
                    var exchangeRate = await GetExchangeRateAsync(currency, "USD");

                    var timestampElements = result.GetProperty("timestamp").EnumerateArray().ToArray();
                    var closeElements = result.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close").EnumerateArray().ToArray();

                    for (int i = 0; i < timestampElements.Length; i++)
                    {
                        var timestampElement = timestampElements[i];
                        var closeElement = closeElements[i];

                        if (timestampElement.ValueKind == JsonValueKind.Number && closeElement.ValueKind == JsonValueKind.Number)
                        {
                            var date = DateTimeOffset.FromUnixTimeSeconds(timestampElement.GetInt64()).UtcDateTime;
                            var priceDate = DateTime.Parse(date.ToString("yyyy-MM-dd"));

                            // Only add if within original date range
                            if (priceDate >= startDate && priceDate <= endDate)
                            {
                                allPrices.Add(new PriceRecord
                                {
                                    Date = date.ToString("yyyy-MM-dd"),
                                    Close = closeElement.GetDecimal() * exchangeRate
                                });
                            }
                        }
                    }

                    currentStart = chunkEnd;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to fetch chunk for {ticker} from {currentStart:yyyy-MM-dd} to {chunkEnd:yyyy-MM-dd}: {ex.Message}");
                    currentStart = chunkEnd; // Move to next chunk on error
                }
            }

            // Remove duplicates and sort by date
            var uniquePrices = allPrices
                .GroupBy(p => p.Date)
                .Select(g => g.First())
                .OrderBy(p => p.Date)
                .ToList();

            if (uniquePrices.Count == 0)
            {
                throw new InvalidOperationException($"No price data found for {ticker} in the requested date range.");
            }

            // Cache the complete result
            cache.Set(cacheKey, uniquePrices, TimeSpan.FromMinutes(30));

            return uniquePrices;
        }
    }
}
