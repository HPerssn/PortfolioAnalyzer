using Microsoft.AspNetCore.Mvc;
using PortfolioAnalyzer.Core.Services;
using PortfolioAnalyzer.Api.Services;

namespace PortfolioAnalyzer.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly RealDataService _dataService;

        public PortfolioController()
        {
            _dataService = new RealDataService();
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
                // Define some sample holdings
                var holdings = new[]
                {
                    new { Symbol = "AAPL", Shares = 10m, AverageCost = 150.00m },
                    new { Symbol = "GOOGL", Shares = 5m, AverageCost = 2500.00m },
                    new { Symbol = "MSFT", Shares = 8m, AverageCost = 300.00m }
                };

                decimal totalValue = 0;
                decimal totalCost = 0;
                var assets = new List<object>();

                foreach (var holding in holdings)
                {
                    var currentPrice = _dataService.GetCurrentPrice(holding.Symbol);
                    var currentValue = holding.Shares * currentPrice;
                    var cost = holding.Shares * holding.AverageCost;
                    var returnAmount = currentValue - cost;
                    var returnPercentage = cost > 0 ? (returnAmount / cost) * 100 : 0;

                    totalValue += currentValue;
                    totalCost += cost;

                    assets.Add(new
                    {
                        Symbol = holding.Symbol,
                        Shares = holding.Shares,
                        AverageCost = Math.Round(holding.AverageCost, 2),
                        CurrentPrice = Math.Round(currentPrice, 2),
                        TotalCost = Math.Round(cost, 2),
                        CurrentValue = Math.Round(currentValue, 2),
                        Return = Math.Round(returnAmount, 2),
                        ReturnPercentage = Math.Round(returnPercentage, 2)
                    });
                }

                var totalReturn = totalValue - totalCost;
                var totalReturnPercentage = totalCost > 0 ? (totalReturn / totalCost) * 100 : 0;

                var summary = new
                {
                    TotalValue = Math.Round(totalValue, 2),
                    TotalCost = Math.Round(totalCost, 2),
                    TotalReturn = Math.Round(totalReturn, 2),
                    TotalReturnPercentage = Math.Round(totalReturnPercentage, 2),
                    AssetCount = holdings.Length,
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
                var holdings = new[]
                {
                    new { Symbol = "AAPL", Shares = 10m, AverageCost = 150.00m },
                    new { Symbol = "GOOGL", Shares = 5m, AverageCost = 2500.00m },
                    new { Symbol = "MSFT", Shares = 8m, AverageCost = 300.00m }
                };

                var assets = new List<object>();

                foreach (var holding in holdings)
                {
                    var currentPrice = _dataService.GetCurrentPrice(holding.Symbol);
                    var currentValue = holding.Shares * currentPrice;
                    var cost = holding.Shares * holding.AverageCost;
                    var returnAmount = currentValue - cost;
                    var returnPercentage = cost > 0 ? (returnAmount / cost) * 100 : 0;

                    assets.Add(new
                    {
                        Symbol = holding.Symbol,
                        Shares = holding.Shares,
                        AverageCost = Math.Round(holding.AverageCost, 2),
                        CurrentPrice = Math.Round(currentPrice, 2),
                        TotalCost = Math.Round(cost, 2),
                        CurrentValue = Math.Round(currentValue, 2),
                        Return = Math.Round(returnAmount, 2),
                        ReturnPercentage = Math.Round(returnPercentage, 2)
                    });
                }

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
