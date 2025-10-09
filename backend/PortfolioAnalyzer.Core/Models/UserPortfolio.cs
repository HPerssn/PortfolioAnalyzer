using System;
using System.Collections.Generic;

namespace PortfolioAnalyzer.Core.Models
{
    /// <summary>
    /// Represents a saved portfolio with metadata
    /// </summary>
    public class UserPortfolio
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime PurchaseDate { get; set; }

        // Navigation property - one portfolio has many holdings
        public List<SavedHolding> Holdings { get; set; } = new List<SavedHolding>();
    }
}