import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { SavedPortfolio } from '@/types/portfolio'

// Use the same environment variable as the rest of the app
const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5129/api'

export const usePortfolioStore = defineStore('portfolio', () => {
  // State
  const savedPortfolios = ref<SavedPortfolio[]>([])
  const selectedPortfolioId = ref<number | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Computed
  const selectedPortfolio = computed(() => {
    if (!selectedPortfolioId.value) return null
    return savedPortfolios.value.find((p) => p.id === selectedPortfolioId.value) || null
  })

  // Actions
  async function fetchPortfolios() {
    isLoading.value = true
    error.value = null
    try {
      const response = await fetch(`${API_BASE}/SavedPortfolios`)
      if (!response.ok) {
        throw new Error(`Failed to fetch portfolios: ${response.statusText}`)
      }
      savedPortfolios.value = await response.json()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Unknown error'
      console.error('Failed to fetch portfolios:', e)
    } finally {
      isLoading.value = false
    }
  }

  async function fetchPortfolioById(id: number) {
    isLoading.value = true
    error.value = null
    try {
      const response = await fetch(`${API_BASE}/SavedPortfolios/${id}`)
      if (!response.ok) {
        throw new Error(`Failed to fetch portfolio: ${response.statusText}`)
      }
      return await response.json()
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Unknown error'
      console.error('Failed to fetch portfolio:', e)
      return null
    } finally {
      isLoading.value = false
    }
  }

  async function savePortfolio(
    name: string,
    purchaseDate: string,
    holdings: Array<{ symbol: string; quantity: number }>,
  ) {
    isLoading.value = true
    error.value = null
    try {
      const response = await fetch(`${API_BASE}/SavedPortfolios`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name,
          purchaseDate,
          holdings,
        }),
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.error || 'Failed to save portfolio')
      }

      const saved = await response.json()
      savedPortfolios.value.push(saved)
      selectedPortfolioId.value = saved.id

      // Save to localStorage
      localStorage.setItem('lastSelectedPortfolioId', saved.id.toString())

      return saved
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Unknown error'
      console.error('Failed to save portfolio:', e)
      throw e
    } finally {
      isLoading.value = false
    }
  }

  async function updatePortfolio(
    id: number,
    name: string,
    purchaseDate: string,
    holdings: Array<{ symbol: string; quantity: number; purchaseDate?: string }>,
  ) {
    isLoading.value = true
    error.value = null
    try {
      const response = await fetch(`${API_BASE}/SavedPortfolios/${id}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          name,
          purchaseDate,
          holdings,
        }),
      })

      if (!response.ok) {
        const errorData = await response.json()
        throw new Error(errorData.error || 'Failed to update portfolio')
      }

      const updated = await response.json()
      const index = savedPortfolios.value.findIndex((p) => p.id === id)
      if (index !== -1) {
        savedPortfolios.value[index] = updated
      }

      return updated
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Unknown error'
      console.error('Failed to update portfolio:', e)
      throw e
    } finally {
      isLoading.value = false
    }
  }

  async function deletePortfolio(id: number) {
    isLoading.value = true
    error.value = null
    try {
      const response = await fetch(`${API_BASE}/SavedPortfolios/${id}`, {
        method: 'DELETE',
      })

      if (!response.ok) {
        throw new Error('Failed to delete portfolio')
      }

      savedPortfolios.value = savedPortfolios.value.filter((p) => p.id !== id)

      if (selectedPortfolioId.value === id) {
        selectedPortfolioId.value = null
        localStorage.removeItem('lastSelectedPortfolioId')
      }
    } catch (e) {
      error.value = e instanceof Error ? e.message : 'Unknown error'
      console.error('Failed to delete portfolio:', e)
      throw e
    } finally {
      isLoading.value = false
    }
  }

  function selectPortfolio(id: number | null) {
    selectedPortfolioId.value = id
    if (id !== null) {
      localStorage.setItem('lastSelectedPortfolioId', id.toString())
    } else {
      localStorage.removeItem('lastSelectedPortfolioId')
    }
  }

  function loadLastSelectedPortfolio() {
    const lastId = localStorage.getItem('lastSelectedPortfolioId')
    if (lastId) {
      selectedPortfolioId.value = parseInt(lastId, 10)
    }
  }

  return {
    // State
    savedPortfolios,
    selectedPortfolioId,
    isLoading,
    error,

    // Computed
    selectedPortfolio,

    // Actions
    fetchPortfolios,
    fetchPortfolioById,
    savePortfolio,
    updatePortfolio,
    deletePortfolio,
    selectPortfolio,
    loadLastSelectedPortfolio,
  }
})