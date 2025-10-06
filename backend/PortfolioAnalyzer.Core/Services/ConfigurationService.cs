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

        public async Task SaveConfigurationAsync(PortfolioConfiguration config, string? filename = null)
        {
            // Auto-generate filename if not provided
            if (string.IsNullOrEmpty(filename))
            {
                filename = $"portfolio_{DateTime.Now:yyyyMMdd_HHmmss}.json";
            }

            // Ensure .json extension
            if (!filename.EndsWith(".json"))
            {
                filename += ".json";
            }

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

        /// <summary>
        /// Creates a new portfolio configuration from a list of tickers
        /// </summary>
        public PortfolioConfiguration CreateConfiguration(
            string name,
            List<TickerConfig> tickers,
            string? description = null)
        {
            return new PortfolioConfiguration
            {
                Name = name,
                Description = description,
                CreatedDate = DateTime.Now,
                LastUpdated = DateTime.Now,
                Tickers = tickers
            };
        }

        /// <summary>
        /// Creates or loads a default configuration for quick testing
        /// </summary>
        public async Task<PortfolioConfiguration> GetOrCreateDefaultConfigurationAsync()
        {
            var defaultConfigName = "default.json";
            var config = await LoadConfigurationAsync(defaultConfigName);

            if (config == null)
            {
                // Create default configuration
                config = new PortfolioConfiguration
                {
                    Name = "Default Portfolio",
                    Description = "Auto-generated default portfolio",
                    Tickers = new List<TickerConfig>
                    {
                        new TickerConfig { Symbol = "AAPL", Quantity = 10, PurchaseDate = DateTime.Now.AddYears(-1) },
                        new TickerConfig { Symbol = "MSFT", Quantity = 15, PurchaseDate = DateTime.Now.AddYears(-1) },
                        new TickerConfig { Symbol = "GOOGL", Quantity = 5, PurchaseDate = DateTime.Now.AddYears(-1) }
                    }
                };

                await SaveConfigurationAsync(config, defaultConfigName);
            }

            return config;
        }

        /// <summary>
        /// Validates a portfolio configuration
        /// </summary>
        public bool ValidateConfiguration(PortfolioConfiguration config, out List<string> errors)
        {
            errors = new List<string>();

            if (string.IsNullOrWhiteSpace(config.Name))
            {
                errors.Add("Portfolio name is required");
            }

            if (config.Tickers == null || config.Tickers.Count == 0)
            {
                errors.Add("Portfolio must contain at least one ticker");
            }
            else
            {
                foreach (var ticker in config.Tickers)
                {
                    if (string.IsNullOrWhiteSpace(ticker.Symbol))
                    {
                        errors.Add("All tickers must have a symbol");
                    }

                    if (ticker.Quantity <= 0)
                    {
                        errors.Add($"Ticker {ticker.Symbol} must have quantity > 0");
                    }

                    if (ticker.PurchaseDate == default)
                    {
                        errors.Add($"Ticker {ticker.Symbol} must have a purchase date");
                    }
                }
            }

            return errors.Count == 0;
        }

        /// <summary>
        /// Deletes a configuration file
        /// </summary>
        public bool DeleteConfiguration(string filename)
        {
            if (!filename.EndsWith(".json"))
            {
                filename += ".json";
            }

            var filePath = Path.Combine(_configDirectory, filename);

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }

            return false;
        }
    }
}
