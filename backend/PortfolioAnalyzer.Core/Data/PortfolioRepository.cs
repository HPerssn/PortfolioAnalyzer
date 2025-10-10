using Microsoft.EntityFrameworkCore;
using PortfolioAnalyzer.Core.Models;

namespace PortfolioAnalyzer.Core.Data
{
    /// <summary>
    /// Repository for portfolio CRUD operations
    /// </summary>
    public class PortfolioRepository
    {
        private readonly PortfolioDbContext _context;

        public PortfolioRepository(PortfolioDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get all saved portfolios with their holdings
        /// </summary>
        public async Task<List<UserPortfolio>> GetAllAsync()
        {
            return await _context.Portfolios
                .Include(p => p.Holdings)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Get a specific portfolio with its holdings
        /// </summary>
        public async Task<UserPortfolio?> GetByIdAsync(int id)
        {
            return await _context.Portfolios
                .Include(p => p.Holdings) // Load related holdings
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        /// <summary>
        /// Create a new portfolio
        /// </summary>
        public async Task<UserPortfolio> CreateAsync(UserPortfolio portfolio)
        {
            _context.Portfolios.Add(portfolio);
            await _context.SaveChangesAsync();
            return portfolio;
        }

        /// <summary>
        /// Update an existing portfolio (replaces name, date, and all holdings)
        /// </summary>
        public async Task<UserPortfolio?> UpdateAsync(int id, string name, DateTime purchaseDate, List<SavedHolding> holdings)
        {
            var portfolio = await _context.Portfolios
                .Include(p => p.Holdings)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (portfolio == null)
                return null;

            // Update portfolio properties
            portfolio.Name = name;
            portfolio.PurchaseDate = purchaseDate;

            // Remove old holdings
            _context.Holdings.RemoveRange(portfolio.Holdings);

            // Add new holdings
            portfolio.Holdings = holdings.Select(h => new SavedHolding
            {
                PortfolioId = id,
                Symbol = h.Symbol,
                Quantity = h.Quantity,
                PurchaseDate = h.PurchaseDate
            }).ToList();

            await _context.SaveChangesAsync();
            return portfolio;
        }

        /// <summary>
        /// Delete a portfolio (holdings are cascade deleted)
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            var portfolio = await _context.Portfolios.FindAsync(id);
            if (portfolio == null)
                return false;

            _context.Portfolios.Remove(portfolio);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
