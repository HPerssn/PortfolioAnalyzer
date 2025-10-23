/**
 * Supported currencies with metadata
 */
export type CurrencyCode = 'USD' | 'EUR' | 'GBP' | 'SEK'

export const CURRENCIES: Record<CurrencyCode, { name: string; symbol: string; locale: string }> = {
  USD: { name: 'US Dollar', symbol: '$', locale: 'en-US' },
  EUR: { name: 'Euro', symbol: '€', locale: 'de-DE' },
  GBP: { name: 'British Pound', symbol: '£', locale: 'en-GB' },
  SEK: { name: 'Swedish Krona', symbol: 'kr', locale: 'sv-SE' },
}

/**
 * Map browser locale to supported currency
 */
export const localeTooCurrency = (locale: string): CurrencyCode => {
  const langPrefix = locale.split('-')[0]

  switch (langPrefix) {
    case 'sv': // Swedish
      return 'SEK'
    case 'de': // German
    case 'fr': // French
    case 'it': // Italian
    case 'nl': // Dutch
    case 'es': // Spanish
    case 'pt': // Portuguese
      return 'EUR'
    case 'en':
      // Check for region code
      if (locale.includes('GB')) return 'GBP'
      return 'USD'
    default:
      return 'USD'
  }
}
