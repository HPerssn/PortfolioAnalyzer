using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            scriptPath = Path.Combine(rootDir, "PortfolioAnalyzer.Core", "Data", "FetchData.py");
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

            // Optional: filter only prices on/after PurchaseDate
            prices = prices.Where(p => DateTime.Parse(p.Date) >= purchaseDate).ToList();

            return prices;
        }
    }
}}
