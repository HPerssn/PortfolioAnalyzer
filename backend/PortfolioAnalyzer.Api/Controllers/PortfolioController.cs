using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PortfolioAnalyzer.Core.Services;
using PortfolioAnalyzer.Core.Models;
using PortfolioAnalyzer.Core.Validators;
using PortfolioAnalyzer.Api.Services;

namespace PortfolioAnalyzer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly RealDataService _dataService;
        private readonly PortfolioBuilderService _portfolioBuilder;
        private readonly ConfigurationService _configService;
        private readonly IMemoryCache _cache;

        public PortfolioController(IMemoryCache cache)
        {
            _dataService = new RealDataService();
            _portfolioBuilder = new PortfolioBuilderService();
            _configService = new ConfigurationService();
            _cache = cache;
        }

        /// <summary>
        /// Get real-time stock price for a symbol
        /// </summary>
        [HttpGet("price/{symbol}")]
        public async Task<IActionResult> GetStockPrice(string symbol)
        {
            try
            {
                // Validate ticker symbol
                if (!TickerValidator.IsValidTickerSymbol(symbol))
                {
                    return BadRequest(new { error = TickerValidator.GetTickerErrorMessage(symbol) });
                }

                var symbolUpper = symbol.ToUpper();
                var price = await _dataService.GetCurrentPriceAsync(symbolUpper);

                // Check if price fetching failed
                if (price == 0)
                {
                    return NotFound(new { error = $"Could not fetch price for symbol '{symbolUpper}'. Symbol may not exist or market data is unavailable." });
                }

                return Ok(new { Symbol = symbolUpper, Price = Math.Round(price, 2), Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to fetch stock price: {ex.Message}" });
            }
        }

        /// <summary>
        /// Request model for calculating portfolio
        /// </summary>
        public class CalculatePortfolioRequest
        {
            public required List<HoldingInput> Holdings { get; set; }
            public required DateTime PurchaseDate { get; set; }
        }

        public class HoldingInput
        {
            public required string Symbol { get; set; }
            public decimal Quantity { get; set; }
        }

        /// <summary>
        /// Calculate portfolio summary from user input (no persistence)
        /// </summary>
        [HttpPost("calculate")]
        public async Task<IActionResult> CalculatePortfolio([FromBody] CalculatePortfolioRequest request)
        {
            try
            {
                if (request?.Holdings == null || !request.Holdings.Any())
                {
                    return BadRequest(new { error = "Holdings are required" });
                }

                // Validate purchase date
                if (!TickerValidator.IsValidPurchaseDate(request.PurchaseDate))
                {
                    return BadRequest(new { error = TickerValidator.GetPurchaseDateErrorMessage(request.PurchaseDate) });
                }

                // Validate each holding
                var validationErrors = new List<string>();
                foreach (var holding in request.Holdings)
                {
                    if (!TickerValidator.IsValidTickerSymbol(holding.Symbol))
                    {
                        validationErrors.Add(TickerValidator.GetTickerErrorMessage(holding.Symbol));
                    }
                    if (!TickerValidator.IsValidQuantity(holding.Quantity))
                    {
                        validationErrors.Add($"{holding.Symbol}: {TickerValidator.GetQuantityErrorMessage(holding.Quantity)}");
                    }
                }

                if (validationErrors.Any())
                {
                    return BadRequest(new { error = "Validation failed", details = validationErrors });
                }

                // Convert to dictionary format
                var holdings = request.Holdings.ToDictionary(h => h.Symbol.ToUpper(), h => h.Quantity);

                var portfolio = await _portfolioBuilder.BuildFromSymbolsAsync(
                    holdings,
                    request.PurchaseDate
                );

                // Build response from portfolio
                var assets = portfolio.Assets.Select(asset => new
                {
                    Symbol = asset.Symbol,
                    Quantity = asset.Quantity,
                    AverageCost = Math.Round(asset.AverageCost, 2),
                    CurrentPrice = Math.Round(asset.CurrentPrice, 2),
                    TotalCost = Math.Round(asset.GetTotalCost(), 2),
                    CurrentValue = Math.Round(asset.GetCurrentValue(), 2),
                    Return = Math.Round(asset.GetReturn(), 2),
                    ReturnPercentage = Math.Round(asset.GetReturnPercentage(), 2)
                }).ToList();

                var summary = new
                {
                    TotalValue = Math.Round(portfolio.GetTotalValue(), 2),
                    TotalCost = Math.Round(portfolio.GetTotalCost(), 2),
                    TotalReturn = Math.Round(portfolio.GetTotalReturn(), 2),
                    TotalReturnPercentage = Math.Round(portfolio.GetTotalReturnPercentage(), 2),
                    AssetCount = portfolio.Assets.Count,
                    Assets = assets,
                    LastUpdated = DateTime.UtcNow
                };

                return Ok(summary);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = $"Portfolio calculation failed: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"An unexpected error occurred: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get portfolio summary with demo data
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetPortfolioSummary()
        {
            try
            {
                // Demo portfolio
                var sampleTickers = new Dictionary<string, decimal>
                {
                    { "AAPL", 10m },
                    { "GOOGL", 5m },
                    { "MSFT", 8m }
                };

                var portfolio = await _portfolioBuilder.BuildFromSymbolsAsync(
                    sampleTickers,
                    new DateTime(2024, 1, 1)
                );

                // Build response from portfolio
                var assets = portfolio.Assets.Select(asset => new
                {
                    Symbol = asset.Symbol,
                    Quantity = asset.Quantity,
                    AverageCost = Math.Round(asset.AverageCost, 2),
                    CurrentPrice = Math.Round(asset.CurrentPrice, 2),
                    TotalCost = Math.Round(asset.GetTotalCost(), 2),
                    CurrentValue = Math.Round(asset.GetCurrentValue(), 2),
                    Return = Math.Round(asset.GetReturn(), 2),
                    ReturnPercentage = Math.Round(asset.GetReturnPercentage(), 2)
                }).ToList();

                var summary = new
                {
                    TotalValue = Math.Round(portfolio.GetTotalValue(), 2),
                    TotalCost = Math.Round(portfolio.GetTotalCost(), 2),
                    TotalReturn = Math.Round(portfolio.GetTotalReturn(), 2),
                    TotalReturnPercentage = Math.Round(portfolio.GetTotalReturnPercentage(), 2),
                    AssetCount = portfolio.Assets.Count,
                    Assets = assets,
                    LastUpdated = DateTime.UtcNow
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get list of all assets with real data
        /// </summary>
        [HttpGet("assets")]
        public async Task<IActionResult> GetAssets()
        {
            try
            {
                // Build a sample portfolio
                var sampleTickers = new Dictionary<string, decimal>
                {
                    { "AAPL", 10m },
                    { "GOOGL", 5m },
                    { "MSFT", 8m }
                };

                var portfolio = await _portfolioBuilder.BuildFromSymbolsAsync(
                    sampleTickers,
                    new DateTime(2024, 1, 1)
                );

                var assets = portfolio.Assets.Select(asset => new
                {
                    Symbol = asset.Symbol,
                    Quantity = asset.Quantity,
                    AverageCost = Math.Round(asset.AverageCost, 2),
                    CurrentPrice = Math.Round(asset.CurrentPrice, 2),
                    TotalCost = Math.Round(asset.GetTotalCost(), 2),
                    CurrentValue = Math.Round(asset.GetCurrentValue(), 2),
                    Return = Math.Round(asset.GetReturn(), 2),
                    ReturnPercentage = Math.Round(asset.GetReturnPercentage(), 2)
                }).ToList();

                return Ok(assets);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get portfolio value history over time
        /// </summary>
        [HttpPost("history")]
        public async Task<IActionResult> GetPortfolioHistory([FromBody] CalculatePortfolioRequest request)
        {
            try
            {
                if (request?.Holdings == null || !request.Holdings.Any())
                {
                    return BadRequest(new { error = "Holdings are required" });
                }

                // Validate purchase date
                if (!TickerValidator.IsValidPurchaseDate(request.PurchaseDate))
                {
                    return BadRequest(new { error = TickerValidator.GetPurchaseDateErrorMessage(request.PurchaseDate) });
                }

                // Validate each holding
                foreach (var holding in request.Holdings)
                {
                    if (!TickerValidator.IsValidTickerSymbol(holding.Symbol))
                    {
                        return BadRequest(new { error = TickerValidator.GetTickerErrorMessage(holding.Symbol) });
                    }
                    if (!TickerValidator.IsValidQuantity(holding.Quantity))
                    {
                        return BadRequest(new { error = TickerValidator.GetQuantityErrorMessage(holding.Quantity) });
                    }
                }

                // Fetch historical prices for all holdings
                var historicalData = new Dictionary<string, List<Core.Data.PriceRecord>>();
                foreach (var holding in request.Holdings)
                {
                    var symbolUpper = holding.Symbol.ToUpper();
                    var prices = await _dataService.GetStockPricesAsync(symbolUpper, request.PurchaseDate);
                    historicalData[symbolUpper] = prices;
                }

                // Find common dates across all holdings
                var allDates = historicalData.Values
                    .SelectMany(prices => prices.Select(p => p.Date))
                    .Distinct()
                    .OrderBy(d => d)
                    .ToList();

                // Calculate portfolio value for each date
                var portfolioHistory = new List<object>();
                foreach (var date in allDates)
                {
                    decimal totalValue = 0;
                    bool hasAllPrices = true;

                    foreach (var holding in request.Holdings)
                    {
                        var symbolUpper = holding.Symbol.ToUpper();
                        var priceOnDate = historicalData[symbolUpper]
                            .FirstOrDefault(p => p.Date == date);

                        if (priceOnDate != null)
                        {
                            totalValue += priceOnDate.Close * holding.Quantity;
                        }
                        else
                        {
                            hasAllPrices = false;
                            break;
                        }
                    }

                    if (hasAllPrices)
                    {
                        portfolioHistory.Add(new
                        {
                            Date = date,
                            Value = Math.Round(totalValue, 2)
                        });
                    }
                }

                return Ok(new { history = portfolioHistory });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to fetch portfolio history: {ex.Message}" });
            }
        }

        /// <summary>
        /// Compare portfolio performance against S&P 500 benchmark
        /// </summary>
        [HttpPost("benchmark")]
        public async Task<IActionResult> GetBenchmarkComparison([FromBody] CalculatePortfolioRequest request)
        {
            try
            {
                if (request?.Holdings == null || !request.Holdings.Any())
                {
                    return BadRequest(new { error = "Holdings are required" });
                }

                // Validate purchase date
                if (!TickerValidator.IsValidPurchaseDate(request.PurchaseDate))
                {
                    return BadRequest(new { error = TickerValidator.GetPurchaseDateErrorMessage(request.PurchaseDate) });
                }

                // Validate each holding
                foreach (var holding in request.Holdings)
                {
                    if (!TickerValidator.IsValidTickerSymbol(holding.Symbol))
                    {
                        return BadRequest(new { error = TickerValidator.GetTickerErrorMessage(holding.Symbol) });
                    }
                    if (!TickerValidator.IsValidQuantity(holding.Quantity))
                    {
                        return BadRequest(new { error = TickerValidator.GetQuantityErrorMessage(holding.Quantity) });
                    }
                }

                // Calculate portfolio performance
                var holdings = request.Holdings.ToDictionary(h => h.Symbol.ToUpper(), h => h.Quantity);
                var portfolio = await _portfolioBuilder.BuildFromSymbolsAsync(holdings, request.PurchaseDate);
                var portfolioReturnPercentage = portfolio.GetTotalReturnPercentage();

                // Fetch S&P 500 benchmark data with caching
                var cacheKey = $"benchmark_sp500_{request.PurchaseDate:yyyyMMdd}";
                decimal benchmarkReturnPercentage;

                if (!_cache.TryGetValue(cacheKey, out benchmarkReturnPercentage))
                {
                    // Fetch S&P 500 (^GSPC) historical data
                    var benchmarkPrices = await _dataService.GetStockPricesAsync("^GSPC", request.PurchaseDate);

                    if (benchmarkPrices == null || benchmarkPrices.Count < 2)
                    {
                        return StatusCode(500, new { error = "Unable to fetch S&P 500 benchmark data" });
                    }

                    // Calculate benchmark return
                    var startPrice = benchmarkPrices.First().Close;
                    var endPrice = benchmarkPrices.Last().Close;
                    benchmarkReturnPercentage = ((endPrice - startPrice) / startPrice) * 100;

                    // Cache for 24 hours
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromHours(24));
                    _cache.Set(cacheKey, benchmarkReturnPercentage, cacheOptions);
                }

                // Calculate difference
                var difference = portfolioReturnPercentage - benchmarkReturnPercentage;
                var daysHeld = (DateTime.Now - request.PurchaseDate).Days;

                var response = new
                {
                    PortfolioReturn = Math.Round(portfolioReturnPercentage, 2),
                    BenchmarkReturn = Math.Round(benchmarkReturnPercentage, 2),
                    Difference = Math.Round(difference, 2),
                    Outperforming = difference > 0,
                    DaysHeld = daysHeld,
                    BenchmarkName = "S&P 500",
                    BenchmarkSymbol = "^GSPC",
                    PurchaseDate = request.PurchaseDate,
                    LastUpdated = DateTime.UtcNow
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Failed to calculate benchmark comparison: {ex.Message}" });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}
