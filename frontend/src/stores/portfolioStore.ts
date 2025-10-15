import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { SavedPortfolio, PortfolioHistoryPoint } from '@/types/portfolio'

// Use the same environment variable as the rest of the app
const API_BASE = import.meta.env.VITE_API_URL || 'http://localhost:5129/api'

export const usePortfolioStore = defineStore('portfolio', () => {
  // State
  const savedPortfolios = ref<SavedPortfolio[]>([])
  const selectedPortfolioId = ref<number | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  // Simulation state
  const simulationState = ref({
    isPlaying: false,
    isPaused: false,
    progress: 0,
    currentIndex: 0,
    timeframeYears: 10,
    speed: 1,
  })
  const simulationData = ref<PortfolioHistoryPoint[]>([])
  const historicalData = ref<PortfolioHistoryPoint[]>([])
  let animationInterval: number | null = null

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

  // Monte Carlo Simulation Functions

  /**
   * Calculate average monthly return from historical data
   */
  function calculateAverageReturn(data: PortfolioHistoryPoint[]): number {
    if (data.length < 2) return 0

    let totalReturn = 0
    for (let i = 1; i < data.length; i++) {
      const monthReturn = (data[i].value - data[i - 1].value) / data[i - 1].value
      totalReturn += monthReturn
    }

    return totalReturn / (data.length - 1)
  }

  /**
   * Calculate volatility (standard deviation of returns) from historical data
   */
  function calculateVolatility(data: PortfolioHistoryPoint[]): number {
    if (data.length < 2) return 0.02 // Default 2% volatility if no data

    const returns: number[] = []
    for (let i = 1; i < data.length; i++) {
      const monthReturn = (data[i].value - data[i - 1].value) / data[i - 1].value
      returns.push(monthReturn)
    }

    const avgReturn = returns.reduce((sum, r) => sum + r, 0) / returns.length
    const variance =
      returns.reduce((sum, r) => sum + Math.pow(r - avgReturn, 2), 0) / returns.length

    return Math.sqrt(variance)
  }

  /**
   * Generate random return using Box-Muller transform for normal distribution
   */
  function generateNormalRandom(mean: number, stdDev: number): number {
    const u1 = Math.random()
    const u2 = Math.random()
    const z0 = Math.sqrt(-2.0 * Math.log(u1)) * Math.cos(2.0 * Math.PI * u2)
    return mean + z0 * stdDev
  }

  /**
   * Generate Monte Carlo simulation data
   */
  function generateSimulationData(
    startingValue: number,
    startDate: Date,
    years: number,
  ): PortfolioHistoryPoint[] {
    const avgReturn = calculateAverageReturn(historicalData.value)
    const volatility = calculateVolatility(historicalData.value)

    const monthsToSimulate = years * 12
    const simulatedPoints: PortfolioHistoryPoint[] = []

    let currentValue = startingValue
    const currentDate = new Date(startDate)

    for (let i = 0; i < monthsToSimulate; i++) {
      // Generate random monthly return using normal distribution
      const monthlyReturn = generateNormalRandom(avgReturn, volatility)
      currentValue = currentValue * (1 + monthlyReturn)

      // Advance date by one month
      currentDate.setMonth(currentDate.getMonth() + 1)

      simulatedPoints.push({
        date: currentDate.toISOString(),
        value: Math.round(currentValue),
      })
    }

    return simulatedPoints
  }

  /**
   * Start simulation
   */
  function startSimulation(years: number, speed: number) {
    // Clear any existing interval
    if (animationInterval) {
      clearInterval(animationInterval)
    }

    // Check if we have historical data
    if (historicalData.value.length === 0) {
      console.warn('No historical data available for simulation')
      return
    }

    // Get the last point from historical data
    const lastHistoricalPoint = historicalData.value[historicalData.value.length - 1]
    const startingValue = lastHistoricalPoint.value
    const startDate = new Date(lastHistoricalPoint.date)

    // Generate simulation data
    const fullSimulation = generateSimulationData(startingValue, startDate, years)
    simulationData.value = fullSimulation

    // Reset simulation state
    simulationState.value = {
      isPlaying: true,
      isPaused: false,
      progress: 0,
      currentIndex: 0,
      timeframeYears: years,
      speed,
    }

    // Start animation
    startAnimation(speed)
  }

  /**
   * Start animation loop
   */
  function startAnimation(speed: number) {
    // Clear any existing interval
    if (animationInterval) {
      clearInterval(animationInterval)
    }

    // Base interval: 100ms per month, adjusted by speed
    const intervalMs = 100 / speed

    animationInterval = window.setInterval(() => {
      if (simulationState.value.currentIndex < simulationData.value.length) {
        simulationState.value.currentIndex++
        simulationState.value.progress = Math.round(
          (simulationState.value.currentIndex / simulationData.value.length) * 100,
        )
      } else {
        // Simulation complete
        pauseSimulation()
      }
    }, intervalMs)
  }

  /**
   * Pause simulation
   */
  function pauseSimulation() {
    if (animationInterval) {
      clearInterval(animationInterval)
      animationInterval = null
    }
    simulationState.value.isPaused = true
  }

  /**
   * Resume simulation
   */
  function resumeSimulation() {
    if (simulationState.value.isPaused && simulationState.value.isPlaying) {
      simulationState.value.isPaused = false
      startAnimation(simulationState.value.speed)
    }
  }

  /**
   * Reset simulation
   */
  function resetSimulation() {
    if (animationInterval) {
      clearInterval(animationInterval)
      animationInterval = null
    }

    simulationState.value = {
      isPlaying: false,
      isPaused: false,
      progress: 0,
      currentIndex: 0,
      timeframeYears: 10,
      speed: 1,
    }

    simulationData.value = []
  }

  /**
   * Update simulation speed while running
   */
  function updateSimulationSpeed(speed: number) {
    simulationState.value.speed = speed
    if (simulationState.value.isPlaying && !simulationState.value.isPaused) {
      startAnimation(speed)
    }
  }

  /**
   * Set historical data for simulation
   */
  function setHistoricalData(data: PortfolioHistoryPoint[]) {
    historicalData.value = data
  }

  /**
   * Get combined chart data (historical + simulation)
   */
  const combinedChartData = computed(() => {
    if (simulationState.value.currentIndex === 0) {
      return historicalData.value
    }

    const visibleSimulation = simulationData.value.slice(
      0,
      simulationState.value.currentIndex,
    )
    return [...historicalData.value, ...visibleSimulation]
  })

  return {
    // State
    savedPortfolios,
    selectedPortfolioId,
    isLoading,
    error,
    simulationState,
    simulationData,
    historicalData,

    // Computed
    selectedPortfolio,
    combinedChartData,

    // Actions
    fetchPortfolios,
    fetchPortfolioById,
    savePortfolio,
    updatePortfolio,
    deletePortfolio,
    selectPortfolio,
    loadLastSelectedPortfolio,

    // Simulation actions
    startSimulation,
    pauseSimulation,
    resumeSimulation,
    resetSimulation,
    updateSimulationSpeed,
    setHistoricalData,
  }
})