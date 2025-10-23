import { defineStore } from 'pinia'
import { ref } from 'vue'
import { type CurrencyCode, CURRENCIES, localeTooCurrency } from '@/constants/currencies'

interface ExchangeRates {
  base: string
  rates: Record<CurrencyCode, number>
  timestamp: number
}

const STORAGE_KEY_CURRENCY = 'selectedCurrency'
const STORAGE_KEY_RATES = 'exchangeRates'
const RATES_CACHE_DURATION = 24 * 60 * 60 * 1000 // 24 hours

export const useCurrencyStore = defineStore('currency', () => {
  // State
  const selectedCurrency = ref<CurrencyCode>('USD')
  const exchangeRates = ref<ExchangeRates | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  /**
   * Initialize currency from localStorage or auto-detect
   */
  function initializeCurrency() {
    // Try to load from localStorage
    const saved = localStorage.getItem(STORAGE_KEY_CURRENCY)
    if (saved && (Object.keys(CURRENCIES) as CurrencyCode[]).includes(saved as CurrencyCode)) {
      selectedCurrency.value = saved as CurrencyCode
      return
    }

    // Auto-detect from browser locale
    const browserLocale = navigator.language || 'en-US'
    selectedCurrency.value = localeTooCurrency(browserLocale)
  }

  /**
   * Set selected currency and persist to localStorage
   */
  function setCurrency(currency: CurrencyCode) {
    selectedCurrency.value = currency
    localStorage.setItem(STORAGE_KEY_CURRENCY, currency)
  }

  /**
   * Fetch exchange rates from API
   */
  async function fetchExchangeRates() {
    isLoading.value = true
    error.value = null

    try {
      // Check if cached rates are still valid
      const cached = localStorage.getItem(STORAGE_KEY_RATES)
      if (cached) {
        const parsed = JSON.parse(cached) as ExchangeRates
        const age = Date.now() - parsed.timestamp
        if (age < RATES_CACHE_DURATION) {
          exchangeRates.value = parsed
          isLoading.value = false
          return
        }
      }

      // Fetch fresh rates (base USD)
      const response = await fetch('https://api.exchangerate-api.io/v4/latest/USD')

      if (!response.ok) {
        throw new Error(`Failed to fetch exchange rates: ${response.statusText}`)
      }

      const data = await response.json()

      // Extract only the currencies we support
      const supportedRates: Record<CurrencyCode, number> = {} as Record<CurrencyCode, number>
      for (const currency of Object.keys(CURRENCIES) as CurrencyCode[]) {
        supportedRates[currency] = data.rates[currency] || 1
      }

      exchangeRates.value = {
        base: 'USD',
        rates: supportedRates,
        timestamp: Date.now(),
      }

      // Cache to localStorage
      localStorage.setItem(STORAGE_KEY_RATES, JSON.stringify(exchangeRates.value))
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Failed to fetch exchange rates'
      console.error('Exchange rates fetch error:', e)

      // Fallback: try to load cached rates even if expired
      const cached = localStorage.getItem(STORAGE_KEY_RATES)
      if (cached) {
        try {
          exchangeRates.value = JSON.parse(cached) as ExchangeRates
          error.value = `Using cached rates (fetch failed)`
        } catch {
          // If cache is corrupted, use default rates
          exchangeRates.value = {
            base: 'USD',
            rates: {
              USD: 1,
              EUR: 0.92,
              GBP: 0.79,
              SEK: 10.5,
            },
            timestamp: Date.now(),
          }
        }
      } else {
        // Use default fallback rates
        exchangeRates.value = {
          base: 'USD',
          rates: {
            USD: 1,
            EUR: 0.92,
            GBP: 0.79,
            SEK: 10.5,
          },
          timestamp: Date.now(),
        }
      }
    } finally {
      isLoading.value = false
    }
  }

  /**
   * Convert amount from USD to selected currency
   */
  function convertToSelectedCurrency(usdAmount: number): number {
    if (!exchangeRates.value) {
      return usdAmount
    }

    const rate = exchangeRates.value.rates[selectedCurrency.value] || 1
    return usdAmount * rate
  }

  /**
   * Get exchange rate for selected currency (relative to USD)
   */
  function getExchangeRate(): number {
    if (!exchangeRates.value) {
      return 1
    }

    return exchangeRates.value.rates[selectedCurrency.value] || 1
  }

  return {
    selectedCurrency,
    exchangeRates,
    isLoading,
    error,
    initializeCurrency,
    setCurrency,
    fetchExchangeRates,
    convertToSelectedCurrency,
    getExchangeRate,
  }
})
