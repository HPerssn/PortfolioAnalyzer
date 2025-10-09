using System;
using System.Text.RegularExpressions;

namespace PortfolioAnalyzer.Core.Validators
{
    /// <summary>
    /// Validates stock ticker symbols and related input data.
    /// </summary>
    public static class TickerValidator
    {
        // Ticker symbols are typically 1-5 uppercase letters (e.g., AAPL, MSFT, GOOGL)
        private static readonly Regex TickerRegex = new Regex(@"^[A-Z]{1,5}$", RegexOptions.Compiled);

        /// <summary>
        /// Validates a stock ticker symbol format.
        /// </summary>
        /// <param name="symbol">The ticker symbol to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidTickerSymbol(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return false;

            return TickerRegex.IsMatch(symbol.Trim().ToUpper());
        }

        /// <summary>
        /// Validates a quantity value.
        /// </summary>
        /// <param name="quantity">The quantity to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidQuantity(decimal quantity)
        {
            return quantity > 0 && quantity <= 1_000_000; // Max 1 million shares
        }

        /// <summary>
        /// Validates a purchase date.
        /// </summary>
        /// <param name="purchaseDate">The purchase date to validate</param>
        /// <returns>True if valid, false otherwise</returns>
        public static bool IsValidPurchaseDate(DateTime purchaseDate)
        {
            // Date must be in the past and not before stock market existed (arbitrarily 1900)
            return purchaseDate < DateTime.Now && purchaseDate.Year >= 1900;
        }

        /// <summary>
        /// Gets a user-friendly error message for an invalid ticker symbol.
        /// </summary>
        /// <param name="symbol">The invalid symbol</param>
        /// <returns>Error message</returns>
        public static string GetTickerErrorMessage(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                return "Ticker symbol cannot be empty";

            if (symbol.Length > 5)
                return $"Ticker symbol '{symbol}' is too long (max 5 characters)";

            if (symbol.Length < 1)
                return "Ticker symbol is too short";

            return $"Invalid ticker symbol '{symbol}'. Must be 1-5 uppercase letters (e.g., AAPL, MSFT)";
        }

        /// <summary>
        /// Gets a user-friendly error message for an invalid quantity.
        /// </summary>
        /// <param name="quantity">The invalid quantity</param>
        /// <returns>Error message</returns>
        public static string GetQuantityErrorMessage(decimal quantity)
        {
            if (quantity <= 0)
                return "Quantity must be greater than 0";

            if (quantity > 1_000_000)
                return "Quantity cannot exceed 1,000,000 shares";

            return $"Invalid quantity: {quantity}";
        }

        /// <summary>
        /// Gets a user-friendly error message for an invalid purchase date.
        /// </summary>
        /// <param name="purchaseDate">The invalid date</param>
        /// <returns>Error message</returns>
        public static string GetPurchaseDateErrorMessage(DateTime purchaseDate)
        {
            if (purchaseDate >= DateTime.Now)
                return "Purchase date cannot be in the future";

            if (purchaseDate.Year < 1900)
                return "Purchase date is too far in the past";

            return $"Invalid purchase date: {purchaseDate:yyyy-MM-dd}";
        }
    }
}
