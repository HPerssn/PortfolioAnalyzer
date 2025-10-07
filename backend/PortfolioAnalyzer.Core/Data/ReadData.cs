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
    public static async Task<List<PriceRecord>> FetchPricesFromYahooFinanceAsync(string ticker, DateTime purchaseDate)
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
    /// <summary>
    /// Calls the Python fetch_prices.py script and returns a list of PriceRecord.
    /// </summary>
    public static List<PriceRecord> FetchPricesFromPython(string ticker, DateTime purchaseDate)
    {
        string startDate = purchaseDate.ToString("yyyy-MM-dd");

        string pythonPath;
        string scriptPath;

        // Check if we're running in Docker (venv at /venv indicates Docker environment)
        if (Directory.Exists("/venv"))
        {
            // Docker environment - use absolute paths
            pythonPath = "/venv/bin/python";
            scriptPath = "/app/PortfolioAnalyzer.Core/Data/FetchData.py";
        }
        else
        {
            // Local development - find venv by traversing up directory tree
            var currentDir = Directory.GetCurrentDirectory();
            var rootDir = currentDir;

            while (!Directory.Exists(Path.Combine(rootDir, "venv")) && rootDir != "/")
            {
                var parent = Directory.GetParent(rootDir);
                if (parent == null) break;
                rootDir = parent.FullName;
            }

            pythonPath = Path.Combine(rootDir, "venv", "bin", "python");
            scriptPath = Path.Combine(rootDir, "backend", "PortfolioAnalyzer.Core", "Data", "FetchData.py");
        }
        
        var psi = new ProcessStartInfo
        {
            FileName = pythonPath,
            Arguments = $"{scriptPath} {ticker} {startDate}",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = Process.Start(psi))
        {
            if (process?.StandardOutput == null)
                throw new InvalidOperationException("Failed to start Python process or read output");

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Deserialize JSON into list of PriceRecord
            var prices = JsonSerializer.Deserialize<List<PriceRecord>>(output);

            // Handle null deserialization result
            if (prices == null)
                return new List<PriceRecord>();

            // filter only prices on/after PurchaseDate
            prices = prices.Where(p => DateTime.Parse(p.Date) >= purchaseDate).ToList();

            return prices;
        }
    }
}}
