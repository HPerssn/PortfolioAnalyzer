/**
 * Currency formatter (Swedish locale, USD currency)
 */
export const formatCurrency = (value: number): string => {
  return new Intl.NumberFormat('sv-SE', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

/**
 * Percentage formatter with + sign for positive values
 */
export const formatPercentage = (value: number): string => {
  return `${value >= 0 ? '+' : ''}${value.toFixed(2)}%`
}

/**
 * Date formatter for charts (e.g., "Jan 15")
 */
export const formatChartDate = (dateString: string): string => {
  const date = new Date(dateString)
  const month = date.toLocaleDateString('en-US', { month: 'short' })
  const day = date.getDate()
  return `${month} ${day}`
}

/**
 * Adaptive date formatter for charts based on timeframe
 * - 1M/3M: "Jan 15" (month + day)
 * - 1Y: "Jan '24" (month + year)
 * - 5Y/All: "2020" (year only)
 */
export const formatChartDateAdaptive = (dateString: string, timeframe: '1M' | '3M' | '1Y' | '5Y' | 'All'): string => {
  const date = new Date(dateString)

  switch (timeframe) {
    case '1M':
    case '3M':
      // Show month and day for shorter timeframes
      const month = date.toLocaleDateString('en-US', { month: 'short' })
      const day = date.getDate()
      return `${month} ${day}`

    case '1Y':
      // Show month and year for 1-year view
      const monthShort = date.toLocaleDateString('en-US', { month: 'short' })
      const year = date.getFullYear().toString().slice(-2)
      return `${monthShort} '${year}`

    case '5Y':
    case 'All':
      // Show year only for 5-year and all-data views
      return date.getFullYear().toString()

    default:
      return formatChartDate(dateString)
  }
}
