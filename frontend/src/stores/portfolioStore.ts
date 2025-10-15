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

  // Multi-path simulation data
  const simulationPaths = ref<PortfolioHistoryPoint[][]>([])
  const simulationPercentiles = ref<{
    p25: PortfolioHistoryPoint[]
    p50: PortfolioHistoryPoint[]
    p75: PortfolioHistoryPoint[]
  }>({
    p25: [],
    p50: [],
    p75: [],
  })

  // Display mode for chart
  const showConfidenceBand = ref(true)

  let animationInterval: number | null = null

  const NUM_SIMULATIONS = 30 // Reduced from 50 for better performance during animation

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

    // Add starting point (last historical point) to ensure continuity
    simulatedPoints.push({
      date: startDate.toISOString(),
      value: Math.round(startingValue),
    })

    let currentValue = startingValue

    for (let i = 0; i < monthsToSimulate; i++) {
      // Generate random monthly return using normal distribution
      const monthlyReturn = generateNormalRandom(avgReturn, volatility)
      currentValue = currentValue * (1 + monthlyReturn)

      // Create a new Date object for each month (avoid mutation)
      const nextDate = new Date(startDate)
      nextDate.setMonth(startDate.getMonth() + i + 1)

      simulatedPoints.push({
        date: nextDate.toISOString(),
        value: Math.round(currentValue),
      })
    }

    return simulatedPoints
  }

  /**
   * Calculate percentile from array of values
   */
  function calculatePercentile(values: number[], percentile: number): number {
    if (values.length === 0) return 0

    const sorted = [...values].sort((a, b) => a - b)
    const index = (percentile / 100) * (sorted.length - 1)
    const lower = Math.floor(index)
    const upper = Math.ceil(index)
    const weight = index % 1

    if (lower === upper) {
      return sorted[lower] ?? 0
    }

    const lowerVal = sorted[lower] ?? 0
    const upperVal = sorted[upper] ?? 0
    return lowerVal * (1 - weight) + upperVal * weight
  }

  /**
   * Calculate percentile bands from multiple simulation paths
   * Simplified to only calculate 25th, 50th, 75th percentiles for better performance
   */
  function calculatePercentileBands(
    paths: PortfolioHistoryPoint[][],
  ): {
    p25: PortfolioHistoryPoint[]
    p50: PortfolioHistoryPoint[]
    p75: PortfolioHistoryPoint[]
  } {
    if (paths.length === 0) {
      return { p25: [], p50: [], p75: [] }
    }

    const numPoints = paths[0].length
    const p25: PortfolioHistoryPoint[] = []
    const p50: PortfolioHistoryPoint[] = []
    const p75: PortfolioHistoryPoint[] = []

    for (let i = 0; i < numPoints; i++) {
      const values = paths.map((path) => path[i]?.value ?? 0)
      const date = paths[0]?.[i]?.date ?? new Date().toISOString()

      p25.push({ date, value: Math.round(calculatePercentile(values, 25)) })
      p50.push({ date, value: Math.round(calculatePercentile(values, 50)) })
      p75.push({ date, value: Math.round(calculatePercentile(values, 75)) })
    }

    return { p25, p50, p75 }
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

    // Generate multiple simulation paths
    const paths: PortfolioHistoryPoint[][] = []
    for (let i = 0; i < NUM_SIMULATIONS; i++) {
      paths.push(generateSimulationData(startingValue, startDate, years))
    }

    // Store only 5 sample paths for visualization (reduce memory usage)
    const sampleIndices = [
      0,
      Math.floor(NUM_SIMULATIONS * 0.25),
      Math.floor(NUM_SIMULATIONS * 0.5),
      Math.floor(NUM_SIMULATIONS * 0.75),
      NUM_SIMULATIONS - 1,
    ]
    simulationPaths.value = sampleIndices
      .map((i) => paths[i])
      .filter((path): path is PortfolioHistoryPoint[] => path !== undefined)

    // Calculate percentile bands
    simulationPercentiles.value = calculatePercentileBands(paths)

    // Use median (p50) as the main simulation data for backward compatibility
    simulationData.value = simulationPercentiles.value.p50

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
   * Start animation loop with optimized frame updates
   * Updates at ~30 FPS instead of 60 FPS to reduce computational load
   */
  function startAnimation(speed: number) {
    // Clear any existing interval
    if (animationInterval) {
      window.cancelAnimationFrame(animationInterval)
    }

    const totalDuration = 10000 // Total animation duration in ms (10 seconds at 1x speed)
    const adjustedDuration = totalDuration / speed
    const startTime = Date.now()
    const totalSteps = simulationData.value.length
    const targetFPS = 30 // Reduce from 60 FPS to 30 FPS for better performance
    const frameInterval = 1000 / targetFPS
    let lastFrameTime = startTime

    const animate = () => {
      const now = Date.now()
      const elapsed = now - startTime
      const timeSinceLastFrame = now - lastFrameTime

      // Only update if enough time has passed (throttle to target FPS)
      if (timeSinceLastFrame >= frameInterval) {
        lastFrameTime = now

        // Calculate progress as a ratio (0 to 1)
        const progressRatio = Math.min(elapsed / adjustedDuration, 1)

        // Calculate current index based on smooth interpolation
        const targetIndex = Math.floor(progressRatio * totalSteps)
        const newIndex = Math.min(targetIndex, totalSteps)

        // Only update if index actually changed (avoid unnecessary reactivity triggers)
        if (newIndex !== simulationState.value.currentIndex) {
          simulationState.value.currentIndex = newIndex
          simulationState.value.progress = Math.round((newIndex / totalSteps) * 100)
        }

        // Check if animation is complete
        if (simulationState.value.currentIndex >= totalSteps) {
          pauseSimulation()
          return
        }
      }

      if (simulationState.value.isPlaying && !simulationState.value.isPaused) {
        animationInterval = window.requestAnimationFrame(animate)
      }
    }

    animationInterval = window.requestAnimationFrame(animate)
  }

  /**
   * Pause simulation
   */
  function pauseSimulation() {
    if (animationInterval) {
      window.cancelAnimationFrame(animationInterval)
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
      window.cancelAnimationFrame(animationInterval)
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
    simulationPaths.value = []
    simulationPercentiles.value = {
      p25: [],
      p50: [],
      p75: [],
    }
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

  /**
   * Toggle confidence band display
   */
  function toggleConfidenceBand() {
    showConfidenceBand.value = !showConfidenceBand.value
  }

  return {
    // State
    savedPortfolios,
    selectedPortfolioId,
    isLoading,
    error,
    simulationState,
    simulationData,
    historicalData,
    simulationPaths,
    simulationPercentiles,
    showConfidenceBand,

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
    toggleConfidenceBand,
  }
})