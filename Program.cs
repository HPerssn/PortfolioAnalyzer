using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class AssetConfig
{
    public string Ticker { get; set; } = "";
    public decimal Quantity { get; set; }
}

public class PortfolioConfig
{
    public List<AssetConfig> Assets { get; set; } = new List<AssetConfig>();
    public DateTime PurchaseDate { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            var configService = new ConfigurationService();
            PortfolioConfiguration? portfolioConfig = null;
            var config = ParseCommandLineArguments(args, out string? configFileName, out bool saveConfig, out bool listConfigs);
            
            // Handle special commands
            if (listConfigs)
            {
                ListAvailableConfigurations(configService);
                return;
            }
            
            // Load from configuration file if specified
            if (!string.IsNullOrEmpty(configFileName))
            {
                Console.WriteLine($"Loading configuration from: {configFileName}");
                portfolioConfig = await configService.LoadConfigurationAsync(configFileName + ".json");
                
                // Override with command line arguments if provided
                if (config.Assets.Any())
                {
                    portfolioConfig.Tickers.Clear();
                    foreach (var asset in config.Assets)
                    {
                        portfolioConfig.Tickers.Add(new TickerConfig
                        {
                            Symbol = asset.Ticker,
                            Quantity = asset.Quantity,
                            PurchaseDate = config.PurchaseDate
                        });
                    }
                }
            }
            else
            {
                // Create portfolio config from command line arguments
                portfolioConfig = new PortfolioConfiguration
                {
                    Name = "Command Line Portfolio",
                    Tickers = config.Assets.Select(a => new TickerConfig
                    {
                        Symbol = a.Ticker,
                        Quantity = a.Quantity,
                        PurchaseDate = config.PurchaseDate
                    }).ToList()
                };
            }
            
            // Create and populate portfolio
            var portfolio = new Portfolio();

            foreach (var tickerConfig in portfolioConfig.Tickers)
            {
                Console.WriteLine($"Fetching data for {tickerConfig.Symbol}...");
                
                // Fetch historical prices from Python script
                var prices = ReadData.FetchPricesFromPython(tickerConfig.Symbol, tickerConfig.PurchaseDate);

                // Create an Asset object
                var asset = new Asset(tickerConfig.Symbol, tickerConfig.PurchaseDate)
                {
                    Quantity = tickerConfig.Quantity,
                    HistoricalPrices = prices.Select(p => p.Close).ToList()
                };

                // Update the ticker config with fetched prices
                tickerConfig.HistoricalPrices = asset.HistoricalPrices;
                if (asset.HistoricalPrices.Any())
                {
                    tickerConfig.PurchasePrice = asset.HistoricalPrices.First();
                }

                // Add the asset to the portfolio
                portfolio.AddAsset(asset);
            }

            // Create the service to handle calculations and display
            var service = new PortfolioService(portfolio);

            // Print portfolio information
            Console.WriteLine($"\n=== {portfolioConfig.Name} ===");
            if (!string.IsNullOrEmpty(portfolioConfig.Description))
            {
                Console.WriteLine($"Description: {portfolioConfig.Description}");
            }
            Console.WriteLine($"Created: {portfolioConfig.CreatedDate:yyyy-MM-dd}");
            Console.WriteLine($"Last Updated: {portfolioConfig.LastUpdated:yyyy-MM-dd}");

            // Print portfolio summary
            service.PrintPortfolioSummary();

            // Print performance metrics
            service.PrintPerformanceMetrics();

            // Print total return
            Console.WriteLine($"Total return: {service.CalculateTotalReturn():C}");

            // Save configuration if requested
            if (saveConfig)
            {
                var configToSave = configService.ConvertFromPortfolio(portfolio, portfolioConfig.Name);
                configToSave.Description = portfolioConfig.Description;
                var saveFileName = !string.IsNullOrEmpty(configFileName) ? configFileName + ".json" : null;
                await configService.SaveConfigurationAsync(configToSave, saveFileName);
            }

            // Keep console open
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            PrintUsage();
        }
    }

    static PortfolioConfig ParseCommandLineArguments(string[] args, out string? configFileName, out bool saveConfig, out bool listConfigs)
    {
        configFileName = null;
        saveConfig = false;
        listConfigs = false;
        
        if (args.Length == 0)
        {
            // Default configuration for demonstration
            return new PortfolioConfig
            {
                PurchaseDate = new DateTime(2024, 1, 1),
                Assets = new List<AssetConfig>
                {
                    new AssetConfig { Ticker = "AAPL", Quantity = 10 },
                    new AssetConfig { Ticker = "GOOG", Quantity = 10 }
                }
            };
        }

        var config = new PortfolioConfig();
        var assets = new List<AssetConfig>();
        
        // Parse arguments
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-d":
                case "--date":
                    if (i + 1 < args.Length && DateTime.TryParse(args[i + 1], out DateTime date))
                    {
                        config.PurchaseDate = date;
                        i++; // Skip the next argument
                    }
                    else
                    {
                        throw new ArgumentException("Invalid date format. Use YYYY-MM-DD");
                    }
                    break;
                    
                case "-a":
                case "--asset":
                    if (i + 1 < args.Length)
                    {
                        var parts = args[i + 1].Split(':');
                        if (parts.Length == 2 && decimal.TryParse(parts[1], out decimal quantity))
                        {
                            assets.Add(new AssetConfig { Ticker = parts[0].ToUpper(), Quantity = quantity });
                        }
                        else
                        {
                            throw new ArgumentException("Invalid asset format. Use TICKER:QUANTITY");
                        }
                        i++; // Skip the next argument
                    }
                    break;
                    
                case "-c":
                case "--config":
                    if (i + 1 < args.Length)
                    {
                        configFileName = args[i + 1];
                        i++; // Skip the next argument
                    }
                    else
                    {
                        throw new ArgumentException("Configuration filename required after -c/--config");
                    }
                    break;
                    
                case "-s":
                case "--save":
                    saveConfig = true;
                    break;
                    
                case "-l":
                case "--list":
                    listConfigs = true;
                    break;
                    
                case "-h":
                case "--help":
                    PrintUsage();
                    Environment.Exit(0);
                    break;
                    
                default:
                    throw new ArgumentException($"Unknown argument: {args[i]}");
            }
        }

        if (config.PurchaseDate == default(DateTime))
        {
            config.PurchaseDate = new DateTime(2024, 1, 1); // Default date
        }

        if (assets.Count == 0 && string.IsNullOrEmpty(configFileName))
        {
            // Default assets if none specified and no config file
            assets.Add(new AssetConfig { Ticker = "AAPL", Quantity = 10 });
            assets.Add(new AssetConfig { Ticker = "GOOG", Quantity = 10 });
        }

        config.Assets = assets;
        return config;
    }

    static void ListAvailableConfigurations(ConfigurationService configService)
    {
        Console.WriteLine("Available portfolio configurations:");
        Console.WriteLine($"Configuration directory: {configService.GetConfigDirectory()}");
        Console.WriteLine();
        
        var configurations = configService.GetAvailableConfigurations();
        
        if (configurations.Any())
        {
            foreach (var config in configurations)
            {
                Console.WriteLine($"  - {config}");
            }
        }
        else
        {
            Console.WriteLine("  No saved configurations found.");
        }
        
        Console.WriteLine();
        Console.WriteLine("Use -c <name> to load a configuration.");
    }

    static void PrintUsage()
    {
        Console.WriteLine("Portfolio Analyzer Usage:");
        Console.WriteLine("dotnet run [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  -d, --date YYYY-MM-DD     Purchase date (default: 2024-01-01)");
        Console.WriteLine("  -a, --asset TICKER:QTY    Add asset with ticker and quantity");
        Console.WriteLine("  -c, --config NAME         Load portfolio configuration from file");
        Console.WriteLine("  -s, --save                Save current portfolio as configuration");
        Console.WriteLine("  -l, --list                List all available configurations");
        Console.WriteLine("  -h, --help                Show this help message");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  dotnet run");
        Console.WriteLine("  dotnet run -d 2023-06-01 -a AAPL:15 -a MSFT:20");
        Console.WriteLine("  dotnet run -c my-portfolio");
        Console.WriteLine("  dotnet run -d 2024-01-01 -a TSLA:5 -s");
        Console.WriteLine("  dotnet run --list");
        Console.WriteLine();
        Console.WriteLine("Configuration Management:");
        Console.WriteLine("  Save portfolio: dotnet run -a AAPL:10 -a GOOG:5 -s");
        Console.WriteLine("  Load portfolio: dotnet run -c portfolio-name");
        Console.WriteLine("  List saved:     dotnet run --list");
    }
}