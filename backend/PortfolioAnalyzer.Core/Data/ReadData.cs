using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

    static ReadData()
    {
        // Add User-Agent to avoid rate limiting from Yahoo Finance
        httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }

    /// <summary>
    /// Fetches stock prices directly from Yahoo Finance HTTP API (no Python required)
    /// </summary>
    public static async Task<List<PriceRecord>> FetchPricesFromYahooFinanceAsync(string ticker, DateTime purchaseDate)
    {
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
            var timestamps = result.GetProperty("timestamp").EnumerateArray().Select(t => t.GetInt64()).ToArray();
            var closes = result.GetProperty("indicators").GetProperty("quote")[0].GetProperty("close").EnumerateArray().Select(c => c.GetDecimal()).ToArray();

            var prices = new List<PriceRecord>();
            for (int i = 0; i < timestamps.Length; i++)
            {
                var date = DateTimeOffset.FromUnixTimeSeconds(timestamps[i]).UtcDateTime;
                prices.Add(new PriceRecord
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Close = closes[i]
                });
            }

            return prices.Where(p => DateTime.Parse(p.Date) >= purchaseDate).ToList();
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
