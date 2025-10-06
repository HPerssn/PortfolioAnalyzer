using System;
using System.Collections.Generic;
using System.Linq;
using PortfolioAnalyzer.Core.Models;
using PortfolioAnalyzer.Core.Data;

namespace PortfolioAnalyzer.Core.Services
{
    /// <summary>
    /// Service for building and populating Portfolio objects with real market data.
    /// Centralizes the logic for fetching historical prices and creating assets.
    /// </summary>
    public class PortfolioBuilderService
    {
        /// <summary>
        /// Builds a portfolio from a complete PortfolioConfiguration.
        /// Fetches historical price data for each ticker and populates the portfolio.
        /// </summary>
        /// <param name="config">The portfolio configuration containing tickers and metadata</param>
        /// <param name="onProgress">Optional callback for progress updates (e.g., "Fetching AAPL...")</param>
        /// <returns>A fully populated Portfolio object</returns>
        public Portfolio BuildFromConfiguration(PortfolioConfiguration config, Action<string>? onProgress = null)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return BuildFromTickers(config.Tickers, onProgress);
        }

        /// <summary>
        /// Builds a portfolio from a list of ticker configurations.
        /// Fetches historical price data for each ticker and populates the portfolio.
        /// </summary>
        /// <param name="tickers">List of ticker configurations</param>
        /// <param name="onProgress">Optional callback for progress updates</param>
        /// <returns>A fully populated Portfolio object</returns>
        public Portfolio BuildFromTickers(List<TickerConfig> tickers, Action<string>? onProgress = null)
        {
            if (tickers == null)
                throw new ArgumentNullException(nameof(tickers));

            var portfolio = new Portfolio();

            foreach (var tickerConfig in tickers)
            {
                // Report progress if callback provided
                onProgress?.Invoke($"Fetching data for {tickerConfig.Symbol}...");

                // Fetch and populate the asset
                var asset = FetchAndPopulateAsset(tickerConfig);

                // Update the ticker config with fetched prices (for saving later)
                tickerConfig.HistoricalPrices = asset.HistoricalPrices;
                if (asset.HistoricalPrices.Any())
                {
                    tickerConfig.PurchasePrice = asset.HistoricalPrices.First();
                }

                // Add to portfolio
                portfolio.AddAsset(asset);
            }

            return portfolio;
        }

        /// <summary>
        /// Fetches historical price data for a single ticker and creates an Asset.
        /// </summary>
        /// <param name="tickerConfig">The ticker configuration</param>
        /// <returns>An Asset populated with historical prices</returns>
        private Asset FetchAndPopulateAsset(TickerConfig tickerConfig)
        {
            // Fetch historical prices from Python/Yahoo Finance
            var prices = ReadData.FetchPricesFromPython(tickerConfig.Symbol, tickerConfig.PurchaseDate);

            // Create the Asset object
            var asset = new Asset(tickerConfig.Symbol, tickerConfig.PurchaseDate)
            {
                Quantity = tickerConfig.Quantity,
                HistoricalPrices = prices.Select(p => p.Close).ToList()
            };

            // Set purchase price and current price if we have data
            if (asset.HistoricalPrices.Any())
            {
                asset.AverageCost = asset.HistoricalPrices.First(); // Purchase price
                asset.CurrentPrice = asset.HistoricalPrices.Last(); // Most recent price
            }

            return asset;
        }

        /// <summary>
        /// Creates a simple portfolio from a list of symbols with a single purchase date.
        /// Useful for quick portfolio creation without full TickerConfig objects.
        /// </summary>
        /// <param name="symbols">Dictionary of symbol -> quantity</param>
        /// <param name="purchaseDate">Purchase date for all assets</param>
        /// <param name="onProgress">Optional callback for progress updates</param>
        /// <returns>A fully populated Portfolio object</returns>
        public Portfolio BuildFromSymbols(Dictionary<string, decimal> symbols, DateTime purchaseDate, Action<string>? onProgress = null)
        {
            var tickers = symbols.Select(kvp => new TickerConfig
            {
                Symbol = kvp.Key,
                Quantity = kvp.Value,
                PurchaseDate = purchaseDate
            }).ToList();

            return BuildFromTickers(tickers, onProgress);
        }
    }
}
