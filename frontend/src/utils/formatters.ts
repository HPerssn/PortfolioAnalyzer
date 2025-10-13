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
