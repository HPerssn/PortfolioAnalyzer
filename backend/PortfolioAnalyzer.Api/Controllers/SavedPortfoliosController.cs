using Microsoft.AspNetCore.Mvc;
using PortfolioAnalyzer.Core.Data;
using PortfolioAnalyzer.Core.Models;

namespace PortfolioAnalyzer.Api.Controllers
{
    /// <summary>
    /// Controller for managing saved portfolios (CRUD operations)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SavedPortfoliosController : ControllerBase
    {
        private readonly PortfolioRepository _repository;

        public SavedPortfoliosController(PortfolioRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Get all saved portfolios (metadata only)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var portfolios = await _repository.GetAllAsync();
                return Ok(portfolios);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve portfolios", details = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific portfolio with its holdings
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var portfolio = await _repository.GetByIdAsync(id);
                if (portfolio == null)
                {
                    return NotFound(new { error = $"Portfolio with ID {id} not found" });
                }
                return Ok(portfolio);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve portfolio", details = ex.Message });
            }
        }

        /// <summary>
        /// Save a new portfolio
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SavePortfolioRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return BadRequest(new { error = "Portfolio name is required" });
                }

                if (request.Holdings == null || request.Holdings.Count == 0)
                {
                    return BadRequest(new { error = "At least one holding is required" });
                }

                var portfolio = new UserPortfolio
                {
                    Name = request.Name,
                    PurchaseDate = request.PurchaseDate,
                    CreatedDate = DateTime.UtcNow,
                    Holdings = request.Holdings.Select(h => new SavedHolding
                    {
                        Symbol = h.Symbol.ToUpper(),
                        Quantity = h.Quantity
                    }).ToList()
                };

                var created = await _repository.CreateAsync(portfolio);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to save portfolio", details = ex.Message });
            }
        }

        /// <summary>
        /// Delete a portfolio
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _repository.DeleteAsync(id);
                if (!success)
                {
                    return NotFound(new { error = $"Portfolio with ID {id} not found" });
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete portfolio", details = ex.Message });
            }
        }
    }

    /// <summary>
    /// Request model for saving a portfolio
    /// </summary>
    public class SavePortfolioRequest
    {
        public required string Name { get; set; }
        public DateTime PurchaseDate { get; set; }
        public required List<HoldingRequest> Holdings { get; set; }
    }

    /// <summary>
    /// Request model for individual holdings
    /// </summary>
    public class HoldingRequest
    {
        public required string Symbol { get; set; }
        public decimal Quantity { get; set; }
    }
}