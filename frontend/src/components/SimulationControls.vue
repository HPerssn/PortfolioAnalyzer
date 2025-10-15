<script setup lang="ts">
import { ref, computed } from 'vue'
import { usePortfolioStore } from '@/stores/portfolioStore'

const portfolioStore = usePortfolioStore()

// Local state for controls
const selectedTimeframe = ref<5 | 10 | 20>(10)
const selectedSpeed = ref<1 | 2 | 5 | 10>(1)

// Computed state from store
const isPlaying = computed(() => portfolioStore.simulationState.isPlaying)
const isPaused = computed(() => portfolioStore.simulationState.isPaused)
const simulationProgress = computed(() => portfolioStore.simulationState.progress)

// Control handlers
const handlePlay = () => {
  if (!isPlaying.value) {
    portfolioStore.startSimulation(selectedTimeframe.value, selectedSpeed.value)
  } else if (isPaused.value) {
    portfolioStore.resumeSimulation()
  }
}

const handlePause = () => {
  portfolioStore.pauseSimulation()
}

const handleReset = () => {
  portfolioStore.resetSimulation()
}

const handleTimeframeChange = (years: 5 | 10 | 20) => {
  selectedTimeframe.value = years
  if (isPlaying.value) {
    // Restart simulation with new timeframe
    portfolioStore.resetSimulation()
    portfolioStore.startSimulation(years, selectedSpeed.value)
  }
}

const handleSpeedChange = (speed: 1 | 2 | 5 | 10) => {
  selectedSpeed.value = speed
  if (isPlaying.value && !isPaused.value) {
    portfolioStore.updateSimulationSpeed(speed)
  }
}

// Status text
const statusText = computed(() => {
  if (!isPlaying.value) return 'Ready to simulate'
  if (isPaused.value) return 'Paused'
  return `Simulating... ${simulationProgress.value}%`
})

// Contextual help text
const contextualText = computed(() => {
  if (!isPlaying.value) {
    return 'Run Monte Carlo projections to explore possible future outcomes'
  }

  // Calculate current year and month
  const totalMonths = selectedTimeframe.value * 12
  const currentMonth = portfolioStore.simulationState.currentIndex
  const currentYear = Math.floor(currentMonth / 12)
  const remainingMonths = currentMonth % 12

  // Get current simulated value
  const simulationData = portfolioStore.combinedChartData
  if (simulationData.length > 0) {
    const currentValue = simulationData[simulationData.length - 1].value
    const startValue = portfolioStore.historicalData.length > 0
      ? portfolioStore.historicalData[portfolioStore.historicalData.length - 1].value
      : 0
    const returnPct = startValue > 0 ? ((currentValue - startValue) / startValue) * 100 : 0
    const sign = returnPct >= 0 ? '+' : ''

    return `Year ${currentYear} of ${selectedTimeframe.value} • Portfolio: $${currentValue.toLocaleString('en-US')} (${sign}${returnPct.toFixed(1)}%)`
  }

  return `Year ${currentYear} of ${selectedTimeframe.value}`
})
</script>

<template>
  <div class="simulation-controls">
    <!-- Section Header -->
    <div class="section-title">
      <h3>SIMULATION</h3>
    </div>

    <div class="controls-layout">
      <!-- Left Column: Controls -->
      <div class="controls-column">
        <!-- Control Buttons -->
        <div class="controls-row">
          <button
            @click="handlePlay"
            class="btn-control btn-play"
            :disabled="isPlaying && !isPaused"
          >
            {{ isPaused ? 'Resume' : 'Play' }}
          </button>
          <button
            @click="handlePause"
            class="btn-control btn-pause"
            :disabled="!isPlaying || isPaused"
          >
            Pause
          </button>
          <button
            @click="handleReset"
            class="btn-control btn-reset"
            :disabled="!isPlaying && !isPaused"
          >
            Reset
          </button>
        </div>

        <!-- Timeframe Selector -->
        <div class="selector-group">
          <label class="selector-label">Timeframe</label>
          <div class="selector-buttons">
            <button
              @click="handleTimeframeChange(5)"
              :class="{ active: selectedTimeframe === 5 }"
              class="btn-selector"
            >
              5Y
            </button>
            <button
              @click="handleTimeframeChange(10)"
              :class="{ active: selectedTimeframe === 10 }"
              class="btn-selector"
            >
              10Y
            </button>
            <button
              @click="handleTimeframeChange(20)"
              :class="{ active: selectedTimeframe === 20 }"
              class="btn-selector"
            >
              20Y
            </button>
          </div>
        </div>

        <!-- Speed Selector -->
        <div class="selector-group">
          <label class="selector-label">Speed</label>
          <div class="selector-buttons">
            <button
              @click="handleSpeedChange(1)"
              :class="{ active: selectedSpeed === 1 }"
              class="btn-selector"
            >
              1x
            </button>
            <button
              @click="handleSpeedChange(2)"
              :class="{ active: selectedSpeed === 2 }"
              class="btn-selector"
            >
              2x
            </button>
            <button
              @click="handleSpeedChange(5)"
              :class="{ active: selectedSpeed === 5 }"
              class="btn-selector"
            >
              5x
            </button>
            <button
              @click="handleSpeedChange(10)"
              :class="{ active: selectedSpeed === 10 }"
              class="btn-selector"
            >
              10x
            </button>
          </div>
        </div>
      </div>

      <!-- Right Column: Status/Context -->
      <div class="status-column">
        <div class="status-text">
          {{ statusText }}
        </div>
        <div class="contextual-text">
          {{ contextualText }}
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.simulation-controls {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
  padding: var(--spacing-xl);
  background: var(--color-card-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  transition: all 0.2s ease;
}

.simulation-controls:hover {
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

.section-title {
  border-bottom: 1px solid var(--color-border);
  padding-bottom: var(--spacing-md);
}

.section-title h3 {
  font-size: var(--font-size-xs);
  font-weight: 500;
  color: var(--color-text-muted);
  margin: 0;
  letter-spacing: 0.05em;
}

.controls-layout {
  display: grid;
  grid-template-columns: auto 1fr;
  gap: var(--spacing-xl);
  align-items: start;
}

.controls-column {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
  min-width: 0;
}

.status-column {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  padding: var(--spacing-lg);
  border-left: 1px solid var(--color-border);
  padding-left: var(--spacing-xl);
  justify-content: center;
  min-height: 120px;
}

.controls-row {
  display: flex;
  gap: var(--spacing-md);
  align-items: center;
  padding-bottom: var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
}

.btn-control {
  padding: 6px 14px;
  border: 1px solid var(--color-border);
  background: transparent;
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  font-weight: 400;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-control:hover:not(:disabled) {
  border-color: #d4d4d4;
}

.btn-control:disabled {
  opacity: 0.3;
  cursor: not-allowed;
}

.btn-play:not(:disabled) {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.btn-play:hover:not(:disabled) {
  border-color: #ea6b0a;
  color: #ea6b0a;
}

.btn-pause,
.btn-reset {
  border: none;
  background: transparent;
  padding: 6px 10px;
  text-decoration: none;
}

.btn-pause:hover:not(:disabled),
.btn-reset:hover:not(:disabled) {
  color: var(--color-text-primary);
  border: none;
}

.selector-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.selector-label {
  font-size: var(--font-size-xs);
  font-weight: 400;
  color: var(--color-text-muted);
}

.selector-buttons {
  display: flex;
  gap: 4px;
}

.btn-selector {
  padding: 6px 14px;
  border: 1px solid var(--color-border);
  background: transparent;
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  font-weight: 400;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-selector:hover {
  border-color: #d4d4d4;
}

.btn-selector.active {
  border-color: var(--color-primary);
  color: var(--color-primary);
  background: transparent;
}

.status-text {
  font-size: var(--font-size-base);
  color: var(--color-text-primary);
  font-weight: 500;
  letter-spacing: -0.01em;
}

.contextual-text {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  font-weight: 400;
  line-height: 1.6;
  transition: opacity 0.2s ease;
}
</style>
