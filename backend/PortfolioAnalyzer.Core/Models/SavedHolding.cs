using System.Text.Json.Serialization;

namespace PortfolioAnalyzer.Core.Models
{
    /// <summary>
    /// Represents a single stock holding within a saved portfolio
    /// </summary>
    public class SavedHolding
    {
        public int Id { get; set; }
        public int PortfolioId { get; set; } // Foreign key to UserPortfolio
        public required string Symbol { get; set; }
        public decimal Quantity { get; set; }

        // Navigation property - belongs to one portfolio
        [JsonIgnore]
        public UserPortfolio? Portfolio { get; set; }
    }
}