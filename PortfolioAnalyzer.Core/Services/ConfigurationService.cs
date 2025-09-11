using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PortfolioAnalyzer.Core.Models;

namespace PortfolioAnalyzer.Core.Services
{
    public class ConfigurationService
    {
        private readonly string _configDirectory;

        public ConfigurationService()
        {
            _configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".portfolioanalyzer");
            if (!Directory.Exists(_configDirectory))
            {
                Directory.CreateDirectory(_configDirectory);
            }
        }

        public string GetConfigDirectory()
        {
            return _configDirectory;
        }

        public async Task<PortfolioConfiguration?> LoadConfigurationAsync(string filename)
        {
            var filePath = Path.Combine(_configDirectory, filename);
            if (!File.Exists(filePath))
                return null;

            var json = await File.ReadAllTextAsync(filePath);
            return JsonSerializer.Deserialize<PortfolioConfiguration>(json);
        }

        public async Task SaveConfigurationAsync(PortfolioConfiguration config, string filename)
        {
            var filePath = Path.Combine(_configDirectory, filename);
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        public List<string> GetAvailableConfigurations()
        {
            var configs = new List<string>();
            if (Directory.Exists(_configDirectory))
            {
                var files = Directory.GetFiles(_configDirectory, "*.json");
                foreach (var file in files)
                {
                    configs.Add(Path.GetFileNameWithoutExtension(file));
                }
            }
            return configs;
        }

        public PortfolioConfiguration ConvertFromPortfolio(Portfolio portfolio, string? name = null)
        {
            var config = new PortfolioConfiguration
            {
                Name = name ?? "Generated Configuration",
                Tickers = new List<TickerConfig>()
            };

            foreach (var asset in portfolio.Assets)
            {
                config.Tickers.Add(new TickerConfig
                {
                    Symbol = asset.Symbol,
                    Quantity = asset.Quantity,
                    Shares = asset.Shares,
                    PurchaseDate = asset.PurchaseDate,
                    HistoricalPrices = asset.HistoricalPrices
                });
            }

            return config;
        }

        public List<Asset> GetSampleAssets()
        {
            // Return sample assets with some realistic data
            return new List<Asset>
            {
                new Asset("AAPL", 10, 150.00m) { CurrentPrice = 175.50m },
                new Asset("GOOGL", 5, 2500.00m) { CurrentPrice = 2750.00m },
                new Asset("MSFT", 8, 300.00m) { CurrentPrice = 350.00m }
            };
        }
    }
}
