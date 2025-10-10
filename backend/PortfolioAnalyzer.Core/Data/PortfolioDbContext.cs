using Microsoft.EntityFrameworkCore;
using PortfolioAnalyzer.Core.Models;

namespace PortfolioAnalyzer.Core.Data
{
    /// <summary>
    /// Database context for portfolio persistence
    /// </summary>
    public class PortfolioDbContext : DbContext
    {
        public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserPortfolio> Portfolios { get; set; }
        public DbSet<SavedHolding> Holdings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure UserPortfolio
            modelBuilder.Entity<UserPortfolio>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Name).IsRequired().HasMaxLength(200);
                entity.Property(p => p.CreatedDate).IsRequired();
                entity.Property(p => p.PurchaseDate).IsRequired();

                // One-to-many relationship
                entity.HasMany(p => p.Holdings)
                      .WithOne(h => h.Portfolio)
                      .HasForeignKey(h => h.PortfolioId)
                      .OnDelete(DeleteBehavior.Cascade); // Delete holdings when portfolio is deleted
            });

            // Configure SavedHolding
            modelBuilder.Entity<SavedHolding>(entity =>
            {
                entity.HasKey(h => h.Id);
                entity.Property(h => h.Symbol).IsRequired().HasMaxLength(19);
                entity.Property(h => h.Quantity).IsRequired().HasPrecision(18, 8);
            });
        }
    }
}