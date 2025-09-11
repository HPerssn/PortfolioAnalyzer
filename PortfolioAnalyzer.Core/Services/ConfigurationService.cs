using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

public class ConfigurationService
{
    private readonly string _configDirectory;
    private const string DefaultConfigFileName = "portfolio.json";

    public ConfigurationService(string? configDirectory = null)
    {
        _configDirectory = configDirectory ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PortfolioAnalyzer");
        
        // Ensure directory exists
        if (!Directory.Exists(_configDirectory))
        {
            Directory.CreateDirectory(_configDirectory);
        }
    }

    public async Task<PortfolioConfiguration> LoadConfigurationAsync(string? fileName = null)
    {
        var filePath = Path.Combine(_configDirectory, fileName ?? DefaultConfigFileName);
        
        if (!File.Exists(filePath))
        {
            // Return default configuration if file doesn't exist
            return CreateDefaultConfiguration();
        }

        try
        {
            var jsonString = await File.ReadAllTextAsync(filePath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            
            var config = JsonSerializer.Deserialize<PortfolioConfiguration>(jsonString, options);
            return config ?? CreateDefaultConfiguration();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading configuration: {ex.Message}");
            return CreateDefaultConfiguration();
        }
    }

    public async Task SaveConfigurationAsync(PortfolioConfiguration config, string? fileName = null)
    {
        var filePath = Path.Combine(_configDirectory, fileName ?? DefaultConfigFileName);
        
        try
        {
            config.LastUpdated = DateTime.Now;
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
            
            var jsonString = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(filePath, jsonString);
            
            Console.WriteLine($"Configuration saved to: {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving configuration: {ex.Message}");
        }
    }

    public List<string> GetAvailableConfigurations()
    {
        var configurations = new List<string>();
        
        try
        {
            var jsonFiles = Directory.GetFiles(_configDirectory, "*.json");
            foreach (var file in jsonFiles)
            {
                configurations.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error listing configurations: {ex.Message}");
        }
        
        return configurations;
    }

    public Portfolio ConvertToPortfolio(PortfolioConfiguration config)
    {
        var portfolio = new Portfolio
        {
            Cash = config.CashBalance
        };

        foreach (var tickerConfig in config.Tickers)
        {
            var asset = new Asset(tickerConfig.Symbol, tickerConfig.PurchaseDate)
            {
                Quantity = tickerConfig.Quantity,
                HistoricalPrices = tickerConfig.HistoricalPrices
            };
            
            portfolio.AddAsset(asset);
        }

        return portfolio;
    }

    public PortfolioConfiguration ConvertFromPortfolio(Portfolio portfolio, string name = "My Portfolio")
    {
        var config = new PortfolioConfiguration
        {
            Name = name,
            CashBalance = portfolio.Cash,
            CreatedDate = DateTime.Now,
            LastUpdated = DateTime.Now
        };

        foreach (var asset in portfolio.Assets)
        {
            var tickerConfig = new TickerConfig
            {
                Symbol = asset.Symbol,
                Quantity = asset.Quantity,
                PurchaseDate = asset.PurchaseDate,
                HistoricalPrices = asset.HistoricalPrices,
                PurchasePrice = asset.HistoricalPrices.FirstOrDefault()
            };
            
            config.Tickers.Add(tickerConfig);
        }

        return config;
    }

    private PortfolioConfiguration CreateDefaultConfiguration()
    {
        return new PortfolioConfiguration
        {
            Name = "Default Portfolio",
            Description = "Default portfolio with AAPL and GOOG",
            Tickers = new List<TickerConfig>
            {
                new TickerConfig
                {
                    Symbol = "AAPL",
                    Quantity = 10,
                    PurchaseDate = new DateTime(2024, 1, 1),
                    Notes = "Apple Inc."
                },
                new TickerConfig
                {
                    Symbol = "GOOG",
                    Quantity = 10,
                    PurchaseDate = new DateTime(2024, 1, 1),
                    Notes = "Alphabet Inc."
                }
            }
        };
    }

    public string GetConfigDirectory() => _configDirectory;
}
