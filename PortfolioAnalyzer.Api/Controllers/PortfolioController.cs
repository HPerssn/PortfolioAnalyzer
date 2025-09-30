using Microsoft.AspNetCore.Mvc;
using PortfolioAnalyzer.Core.Services;
using PortfolioAnalyzer.Core.Models;
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
                var price = _dataService.GetCurrentPrice(symbol.ToUpper());
                return Ok(new { Symbol = symbol.ToUpper(), Price = Math.Round(price, 2), Timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get portfolio summary with real data
        /// </summary>
        [HttpGet("summary")]
        public IActionResult GetPortfolioSummary()
        {
            try
            {
                // Build a sample portfolio (or load from config)
                // In a real app, this would come from user's saved portfolio or request parameters
                var sampleTickers = new Dictionary<string, decimal>
                {
                    { "AAPL", 10m },
                    { "GOOGL", 5m },
                    { "MSFT", 8m }
                };

                var portfolio = _portfolioBuilder.BuildFromSymbols(
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
        public IActionResult GetAssets()
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

                var portfolio = _portfolioBuilder.BuildFromSymbols(
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
