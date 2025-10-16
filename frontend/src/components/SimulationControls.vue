<script setup lang="ts">
import { ref, computed } from 'vue'
import { usePortfolioStore } from '@/stores/portfolioStore'

const portfolioStore = usePortfolioStore()

// Info modal state
const showInfo = ref(false)

const toggleInfo = () => {
  showInfo.value = !showInfo.value
}

// Local state for controls
const selectedTimeframe = ref<5 | 10 | 20>(10)
const selectedSpeed = ref<1 | 2 | 5 | 10>(1)

// Computed state from store
const isPlaying = computed(() => portfolioStore.simulationState.isPlaying)
const isPaused = computed(() => portfolioStore.simulationState.isPaused)
const simulationProgress = computed(() => portfolioStore.simulationState.progress)
const showConfidenceBand = computed(() => portfolioStore.showConfidenceBand)

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

const handleToggleConfidenceBand = () => {
  portfolioStore.toggleConfidenceBand()
}

// Status text
const statusText = computed(() => {
  if (!isPlaying.value) return 'Ready to simulate'
  if (isPaused.value && simulationProgress.value === 100) return 'Simulation complete'
  if (isPaused.value) return 'Paused'
  if (simulationProgress.value === 100) return 'Simulation complete'
  return `Simulating... ${simulationProgress.value}%`
})

// Contextual help text - less number-focused, more qualitative
const contextualText = computed(() => {
  if (!isPlaying.value) {
    return 'Explore how your portfolio might evolve over time based on historical patterns'
  }

  // Check if simulation is complete
  const isComplete = simulationProgress.value === 100

  // Calculate current year and month
  const currentMonth = portfolioStore.simulationState.currentIndex
  const currentYear = Math.floor(currentMonth / 12)
  const currentMonthInYear = currentMonth % 12

  // Get current simulated value from median (p50)
  const simulationData = portfolioStore.simulationPercentiles.p50
  if (simulationData.length > 0 && currentMonth > 0) {
    const dataIndex = Math.min(currentMonth - 1, simulationData.length - 1)
    const currentValue = simulationData[dataIndex]?.value ?? 0
    const startValue = portfolioStore.historicalData.length > 0
      ? portfolioStore.historicalData[portfolioStore.historicalData.length - 1]?.value ?? 0
      : 0
    const returnPct = startValue > 0 ? ((currentValue - startValue) / startValue) * 100 : 0

    // Qualitative descriptions instead of exact percentages
    const getTrajectoryDescription = (pct: number) => {
      if (pct > 100) return 'strong growth trajectory'
      if (pct > 50) return 'solid upward path'
      if (pct > 20) return 'steady growth'
      if (pct > 5) return 'modest gains'
      if (pct > -5) return 'holding steady'
      if (pct > -20) return 'slight downturn'
      return 'challenging period'
    }

    if (isComplete) {
      const trajectory = getTrajectoryDescription(returnPct)

      // Get final percentile values for range
      const finalP25 = portfolioStore.simulationPercentiles.p25[portfolioStore.simulationPercentiles.p25.length - 1]?.value ?? 0
      const finalP75 = portfolioStore.simulationPercentiles.p75[portfolioStore.simulationPercentiles.p75.length - 1]?.value ?? 0

      const variationPct = startValue > 0 ? ((finalP75 - finalP25) / startValue) * 100 : 0
      const certainty = variationPct < 30 ? 'relatively stable' : variationPct < 80 ? 'moderate uncertainty' : 'wide range of possibilities'

      return `${selectedTimeframe.value}-year outlook shows ${trajectory} • ${certainty}`
    }

    const trajectory = getTrajectoryDescription(returnPct)
    const yearProgress = currentMonthInYear === 0
      ? `Beginning of year ${currentYear + 1}`
      : `Year ${currentYear + 1}, month ${currentMonthInYear}`

    return `${yearProgress} • Currently showing ${trajectory}`
  }

  return isComplete
    ? `${selectedTimeframe.value}-year projection complete`
    : `${currentYear > 0 ? `Year ${currentYear}` : 'Starting'} of ${selectedTimeframe.value}`
})
</script>

<template>
  <div class="simulation-controls">
    <!-- Section Header -->
    <div class="section-title">
      <h3>SIMULATION</h3>
      <button class="info-button" @click="toggleInfo" aria-label="Information about Monte Carlo simulation">
        <span class="info-icon">i</span>
      </button>
    </div>

    <!-- Info Modal -->
    <div v-if="showInfo" class="info-modal" @click.self="toggleInfo">
      <div class="info-modal-content">
        <button class="close-button" @click="toggleInfo">&times;</button>
        <h4>Monte Carlo Simulation</h4>
        <p class="info-text">
          Watch your portfolio grow forward in time. Each simulation uses historical patterns to project possible futures. not predictions, just possibilities.
        </p>
        <p class="info-text">
          The median path shows the middle outcome. The range shows where most scenarios land. This helps you understand what patience might look like.
        </p>
        <p class="info-text">
          Use this to think in years, not days.
        </p>
      </div>
    </div>

    <!-- Unified Layout -->
    <div class="controls-layout">
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

      <!-- Options Row -->
      <div class="options-row">
        <!-- Timeframe -->
        <div class="option-group">
          <label class="option-label">Timeframe</label>
          <div class="option-buttons">
            <button
              @click="handleTimeframeChange(5)"
              :class="{ active: selectedTimeframe === 5 }"
              class="btn-option"
            >
              5Y
            </button>
            <button
              @click="handleTimeframeChange(10)"
              :class="{ active: selectedTimeframe === 10 }"
              class="btn-option"
            >
              10Y
            </button>
            <button
              @click="handleTimeframeChange(20)"
              :class="{ active: selectedTimeframe === 20 }"
              class="btn-option"
            >
              20Y
            </button>
          </div>
        </div>

        <!-- Speed -->
        <div class="option-group">
          <label class="option-label">Speed</label>
          <div class="option-buttons">
            <button
              @click="handleSpeedChange(1)"
              :class="{ active: selectedSpeed === 1 }"
              class="btn-option"
            >
              1x
            </button>
            <button
              @click="handleSpeedChange(2)"
              :class="{ active: selectedSpeed === 2 }"
              class="btn-option"
            >
              2x
            </button>
            <button
              @click="handleSpeedChange(5)"
              :class="{ active: selectedSpeed === 5 }"
              class="btn-option"
            >
              5x
            </button>
            <button
              @click="handleSpeedChange(10)"
              :class="{ active: selectedSpeed === 10 }"
              class="btn-option"
            >
              10x
            </button>
          </div>
        </div>

        <!-- Display Toggle -->
        <div class="option-group">
          <label class="option-label">Display</label>
          <button
            @click="handleToggleConfidenceBand"
            :class="{ active: showConfidenceBand }"
            class="btn-toggle"
          >
            Range
          </button>
        </div>
      </div>

      <!-- Status Text -->
      <div class="status-section">
        <div class="status-text">{{ statusText }}</div>
        <div class="contextual-text">{{ contextualText }}</div>
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
}

.section-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-bottom: var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
}

.section-title h3 {
  font-size: var(--font-size-xs);
  font-weight: 500;
  color: var(--color-text-muted);
  margin: 0;
  letter-spacing: 0.05em;
}

.info-button {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  border: 1px solid var(--color-border);
  background: transparent;
  color: var(--color-text-muted);
  cursor: pointer;
  transition: all 0.2s;
  padding: 0;
}

.info-button:hover {
  border-color: var(--color-primary);
  color: var(--color-primary);
  background: rgba(249, 115, 22, 0.05);
}

.info-icon {
  font-size: 11px;
  font-weight: 600;
  font-family: serif;
  font-style: italic;
  line-height: 1;
}

.info-modal {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  padding: var(--spacing-xl);
}

.info-modal-content {
  background: var(--color-card-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--spacing-xl);
  max-width: 500px;
  width: 100%;
  position: relative;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
}

.info-modal-content h4 {
  font-size: var(--font-size-base);
  font-weight: 500;
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-md) 0;
}

.info-text {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  line-height: 1.6;
  margin: 0 0 var(--spacing-md) 0;
}

.info-text:last-of-type {
  margin-bottom: 0;
}

.close-button {
  position: absolute;
  top: var(--spacing-md);
  right: var(--spacing-md);
  background: transparent;
  border: none;
  font-size: 24px;
  color: var(--color-text-muted);
  cursor: pointer;
  padding: 0;
  width: 24px;
  height: 24px;
  display: flex;
  align-items: center;
  justify-content: center;
  line-height: 1;
  transition: color 0.2s;
}

.close-button:hover {
  color: var(--color-text-primary);
}

.controls-layout {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
}

.controls-row {
  display: flex;
  gap: var(--spacing-md);
  align-items: center;
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
  border-color: var(--color-border-hover);
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
  border-color: var(--color-primary-hover);
  color: var(--color-primary-hover);
}

.btn-pause,
.btn-reset {
  border: none;
  background: transparent;
  padding: 6px 10px;
}

.btn-pause:hover:not(:disabled),
.btn-reset:hover:not(:disabled) {
  color: var(--color-text-primary);
  border: none;
}

.options-row {
  display: flex;
  gap: var(--spacing-xl);
  align-items: flex-start;
  flex-wrap: wrap;
}

.option-group {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.option-label {
  font-size: var(--font-size-xs);
  font-weight: 400;
  color: var(--color-text-muted);
}

.option-buttons {
  display: flex;
  gap: 4px;
}

.btn-option {
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

.btn-option:hover {
  border-color: var(--color-border-hover);
}

.btn-option.active {
  border-color: var(--color-primary);
  color: var(--color-primary);
  background: transparent;
}

.btn-toggle {
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

.btn-toggle:hover {
  border-color: var(--color-border-hover);
}

.btn-toggle.active {
  border-color: var(--color-primary);
  color: var(--color-primary);
  background: transparent;
}

.status-section {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
  padding-top: var(--spacing-md);
  border-top: 1px solid var(--color-border);
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
}
</style>
