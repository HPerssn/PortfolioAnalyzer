using Microsoft.AspNetCore.Mvc;
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

        public PortfolioController()
        {
            _dataService = new RealDataService();
            _portfolioBuilder = new PortfolioBuilderService();
            _configService = new ConfigurationService();
        }

        /// <summary>
        /// Get real-time stock price for a symbol
        /// </summary>
        [HttpGet("price/{symbol}")]
        public IActionResult GetStockPrice(string symbol)
        {
            try
            {
                // Validate ticker symbol
                if (!TickerValidator.IsValidTickerSymbol(symbol))
                {
                    return BadRequest(new { error = TickerValidator.GetTickerErrorMessage(symbol) });
                }

                var symbolUpper = symbol.ToUpper();
                var price = _dataService.GetCurrentPrice(symbolUpper);

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
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}
