import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { PortfolioHistoryPoint } from '@/types/portfolio'

const NUM_SIMULATIONS = 30

export const useMonteCarloStore = defineStore('monteCarlo', () => {
  // State
  const simulationState = ref({
    isPlaying: false,
    isPaused: false,
    isComplete: false,
    progress: 0,
    currentIndex: 0,
    timeframeYears: 10,
    speed: 1,
  })
  const simulationData = ref<PortfolioHistoryPoint[]>([])
  const historicalData = ref<PortfolioHistoryPoint[]>([])
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
  const showConfidenceBand = ref(true)

  let animationInterval: number | null = null
  let animationStartTime = 0 // When animation started (absolute)
  let totalPausedDuration = 0 // Total time spent paused
  let pauseStartTime = 0 // When pause started

  // Calculated average monthly return
  function calculateAverageReturn(data: PortfolioHistoryPoint[]): number {
    if (data.length < 2) return 0
    let totalReturn = 0
    let validPeriods = 0
    for (let i = 1; i < data.length; i++) {
      const current = data[i]
      const previous = data[i - 1]
      if (current && previous && previous.value !== 0) {
        totalReturn += (current.value - previous.value) / previous.value
        validPeriods++
      }
    }
    return validPeriods > 0 ? totalReturn / validPeriods : 0
  }

  // Calculate volatility
  function calculateVolatility(data: PortfolioHistoryPoint[]): number {
    if (data.length < 2) return 0.02
    const returns: number[] = []
    for (let i = 1; i < data.length; i++) {
      const current = data[i]
      const previous = data[i - 1]
      if (current && previous && previous.value !== 0) {
        returns.push((current.value - previous.value) / previous.value)
      }
    }
    if (returns.length < 2) return 0.02
    const avg = returns.reduce((a, b) => a + b, 0) / returns.length
    const variance = returns.reduce((a, b) => a + Math.pow(b - avg, 2), 0) / returns.length
    return Math.sqrt(variance)
  }

  // Generate normal random using Box-Muller
  function generateNormalRandom(mean: number, stdDev: number): number {
    const u1 = Math.random()
    const u2 = Math.random()
    const z0 = Math.sqrt(-2 * Math.log(u1)) * Math.cos(2 * Math.PI * u2)
    return mean + z0 * stdDev
  }

  // Generate simulation data
  function generateSimulationData(
    startingValue: number,
    startDate: Date,
    years: number,
  ): PortfolioHistoryPoint[] {
    const avgReturn = calculateAverageReturn(historicalData.value)
    const volatility = calculateVolatility(historicalData.value)
    const monthsToSimulate = years * 12
    const points: PortfolioHistoryPoint[] = []

    points.push({ date: startDate.toISOString(), value: Math.round(startingValue) })

    let currentValue = startingValue
    for (let i = 0; i < monthsToSimulate; i++) {
      const monthlyReturn = generateNormalRandom(avgReturn, volatility)
      currentValue = currentValue * (1 + monthlyReturn)
      const nextDate = new Date(startDate)
      nextDate.setMonth(startDate.getMonth() + i + 1)
      points.push({ date: nextDate.toISOString(), value: Math.round(currentValue) })
    }

    return points
  }

  // Calculate percentile
  function calculatePercentile(values: number[], percentile: number): number {
    if (values.length === 0) return 0
    const sorted = [...values].sort((a, b) => a - b)
    const index = (percentile / 100) * (sorted.length - 1)
    const lower = Math.floor(index)
    const upper = Math.ceil(index)
    const weight = index % 1
    if (lower === upper) return sorted[lower] ?? 0
    return (sorted[lower] ?? 0) * (1 - weight) + (sorted[upper] ?? 0) * weight
  }

  // Calculate percentile bands
  function calculatePercentileBands(
    paths: PortfolioHistoryPoint[][],
  ): {
    p25: PortfolioHistoryPoint[]
    p50: PortfolioHistoryPoint[]
    p75: PortfolioHistoryPoint[]
  } {
    const firstPath = paths[0]
    if (!firstPath) return { p25: [], p50: [], p75: [] }

    const numPoints = firstPath.length
    const p25: PortfolioHistoryPoint[] = []
    const p50: PortfolioHistoryPoint[] = []
    const p75: PortfolioHistoryPoint[] = []

    for (let i = 0; i < numPoints; i++) {
      const values = paths.map((path) => path[i]?.value ?? 0)
      const date = firstPath[i]?.date ?? new Date().toISOString()
      p25.push({ date, value: Math.round(calculatePercentile(values, 25)) })
      p50.push({ date, value: Math.round(calculatePercentile(values, 50)) })
      p75.push({ date, value: Math.round(calculatePercentile(values, 75)) })
    }

    return { p25, p50, p75 }
  }

  // Start simulation
  function startSimulation(years: number, speed: number) {
    if (animationInterval) clearInterval(animationInterval)

    if (historicalData.value.length === 0) {
      console.warn('Cannot start simulation: No historical data available')
      return
    }

    const lastPoint = historicalData.value[historicalData.value.length - 1]
    if (!lastPoint) {
      console.error('Cannot start simulation: No last historical point')
      return
    }

    const paths: PortfolioHistoryPoint[][] = []
    for (let i = 0; i < NUM_SIMULATIONS; i++) {
      paths.push(generateSimulationData(lastPoint.value, new Date(lastPoint.date), years))
    }

    const sampleIndices = [
      0,
      Math.floor(NUM_SIMULATIONS * 0.25),
      Math.floor(NUM_SIMULATIONS * 0.5),
      Math.floor(NUM_SIMULATIONS * 0.75),
      NUM_SIMULATIONS - 1,
    ]
    simulationPaths.value = sampleIndices
      .map((i) => paths[i])
      .filter((p): p is PortfolioHistoryPoint[] => p !== undefined)

    simulationPercentiles.value = calculatePercentileBands(paths)
    simulationData.value = simulationPercentiles.value.p50

    // Reset timing variables for new simulation
    animationStartTime = 0
    totalPausedDuration = 0
    pauseStartTime = 0

    simulationState.value = {
      isPlaying: true,
      isPaused: false,
      isComplete: false,
      progress: 0,
      currentIndex: 0,
      timeframeYears: years,
      speed,
    }

    startAnimation(speed)
  }

  // Animation loop
  function startAnimation(speed: number) {
    if (animationInterval) window.cancelAnimationFrame(animationInterval)

    // Initialize animation start time on first call
    if (animationStartTime === 0) {
      animationStartTime = Date.now()
    }

    const totalDuration = 10000
    const adjustedDuration = totalDuration / speed
    const totalSteps = simulationData.value.length
    const frameInterval = 1000 / 30
    let lastFrameTime = Date.now()

    const animate = () => {
      const now = Date.now()
      // Calculate elapsed time, accounting for pauses
      const elapsed = (now - animationStartTime - totalPausedDuration)
      const timeSinceLastFrame = now - lastFrameTime

      if (timeSinceLastFrame >= frameInterval) {
        lastFrameTime = now
        const progressRatio = Math.min(elapsed / adjustedDuration, 1)
        const newIndex = Math.min(Math.floor(progressRatio * totalSteps), totalSteps)

        if (newIndex !== simulationState.value.currentIndex) {
          simulationState.value.currentIndex = newIndex
          simulationState.value.progress = Math.round((newIndex / totalSteps) * 100)
        }

        if (newIndex >= totalSteps) {
          // Simulation complete - keep result visible
          simulationState.value.isPlaying = false
          simulationState.value.isPaused = false
          simulationState.value.isComplete = true
          return
        }
      }

      if (simulationState.value.isPlaying && !simulationState.value.isPaused) {
        animationInterval = window.requestAnimationFrame(animate)
      }
    }

    animationInterval = window.requestAnimationFrame(animate)
  }

  function pauseSimulation() {
    if (animationInterval) {
      window.cancelAnimationFrame(animationInterval)
      animationInterval = null
    }
    // Record when pause started
    pauseStartTime = Date.now()
    simulationState.value.isPaused = true
  }

  function resumeSimulation() {
    if (simulationState.value.isPaused && simulationState.value.isPlaying) {
      // Add the paused duration to our total
      if (pauseStartTime > 0) {
        totalPausedDuration += (Date.now() - pauseStartTime)
        pauseStartTime = 0
      }
      simulationState.value.isPaused = false
      startAnimation(simulationState.value.speed)
    }
  }

  function resetSimulation() {
    if (animationInterval) {
      window.cancelAnimationFrame(animationInterval)
      animationInterval = null
    }

    // Reset timing variables
    animationStartTime = 0
    totalPausedDuration = 0
    pauseStartTime = 0

    simulationState.value = {
      isPlaying: false,
      isPaused: false,
      isComplete: false,
      progress: 0,
      currentIndex: 0,
      timeframeYears: 10,
      speed: 1,
    }

    simulationData.value = []
    simulationPaths.value = []
    simulationPercentiles.value = { p25: [], p50: [], p75: [] }
  }

  function updateSimulationSpeed(speed: number) {
    simulationState.value.speed = speed
    if (simulationState.value.isPlaying && !simulationState.value.isPaused) {
      startAnimation(speed)
    }
  }

  function setHistoricalData(data: PortfolioHistoryPoint[]) {
    historicalData.value = data
  }

  function toggleConfidenceBand() {
    showConfidenceBand.value = !showConfidenceBand.value
  }

  // Computed
  const combinedChartData = computed(() => {
    if (simulationState.value.currentIndex === 0) {
      return historicalData.value
    }
    return [...historicalData.value, ...simulationData.value.slice(0, simulationState.value.currentIndex)]
  })

  return {
    simulationState,
    simulationData,
    historicalData,
    simulationPaths,
    simulationPercentiles,
    showConfidenceBand,
    combinedChartData,
    startSimulation,
    pauseSimulation,
    resumeSimulation,
    resetSimulation,
    updateSimulationSpeed,
    setHistoricalData,
    toggleConfidenceBand,
  }
})
