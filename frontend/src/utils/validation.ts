/**
 * Validates a stock ticker symbol format.
 * Ticker symbols are typically 1-5 uppercase letters (e.g., AAPL, MSFT, GOOGL)
 */
export function isValidTickerSymbol(symbol: string): boolean {
  if (!symbol || symbol.trim() === '') return false
  const tickerRegex = /^[A-Z]{1,5}$/
  return tickerRegex.test(symbol.trim().toUpperCase())
}

/**
 * Validates a quantity value.
 */
export function isValidQuantity(quantity: number): boolean {
  return quantity > 0 && quantity <= 1_000_000
}

/**
 * Validates a purchase date.
 */
export function isValidPurchaseDate(date: string): boolean {
  const purchaseDate = new Date(date)
  const now = new Date()
  const minDate = new Date('1900-01-01')

  return purchaseDate < now && purchaseDate >= minDate
}

/**
 * Gets a user-friendly error message for an invalid ticker symbol.
 */
export function getTickerErrorMessage(symbol: string): string {
  if (!symbol || symbol.trim() === '') {
    return 'Ticker symbol cannot be empty'
  }

  const trimmed = symbol.trim()

  if (trimmed.length > 5) {
    return `Ticker symbol '${trimmed}' is too long (max 5 characters)`
  }

  if (trimmed.length < 1) {
    return 'Ticker symbol is too short'
  }

  return `Invalid ticker symbol '${trimmed}'. Must be 1-5 uppercase letters (e.g., AAPL, MSFT)`
}

/**
 * Gets a user-friendly error message for an invalid quantity.
 */
export function getQuantityErrorMessage(quantity: number): string {
  if (quantity <= 0) {
    return 'Quantity must be greater than 0'
  }

  if (quantity > 1_000_000) {
    return 'Quantity cannot exceed 1,000,000 shares'
  }

  return `Invalid quantity: ${quantity}`
}

/**
 * Gets a user-friendly error message for an invalid purchase date.
 */
export function getPurchaseDateErrorMessage(date: string): string {
  const purchaseDate = new Date(date)
  const now = new Date()

  if (purchaseDate >= now) {
    return 'Purchase date cannot be in the future'
  }

  if (purchaseDate.getFullYear() < 1900) {
    return 'Purchase date is too far in the past'
  }

  return `Invalid purchase date: ${date}`
}
