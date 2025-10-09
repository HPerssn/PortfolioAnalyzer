/**
 * Validates a stock ticker symbol format.
 * Supports international tickers with hyphens and dots (e.g., AAPL, INVE-B.ST, BRK.A)
 */
export function isValidTickerSymbol(symbol: string): boolean {
  if (!symbol || symbol.trim() === '') return false
  const tickerRegex = /^[A-Z0-9][A-Z0-9.\-]{0,19}$/
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

  if (trimmed.length > 10) {
    return `Ticker symbol '${trimmed}' is too long (max 10 characters)`
  }

  if (trimmed.length < 1) {
    return 'Ticker symbol is too short'
  }

  return `Invalid ticker symbol '${trimmed}'. Must be 1-10 uppercase letters/numbers/dots/hyphens (e.g., AAPL, INVE-B.ST)`
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
