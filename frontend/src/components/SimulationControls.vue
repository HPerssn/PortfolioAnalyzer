<script setup lang="ts">
import { ref, computed } from 'vue'
import { useMonteCarloStore } from '@/stores/monteCarloStore'

const monteCarloStore = useMonteCarloStore()

// Info state
const showInfo = ref(false)


// Local state for controls
const selectedTimeframe = ref<5 | 10 | 20>(10)
const selectedSpeed = ref<1 | 2 | 5 | 10>(1)

// Computed state from store
const isPlaying = computed(() => monteCarloStore.simulationState.isPlaying)
const isPaused = computed(() => monteCarloStore.simulationState.isPaused)
const simulationProgress = computed(() => monteCarloStore.simulationState.progress)
const showConfidenceBand = computed(() => monteCarloStore.showConfidenceBand)

// Control handlers
const handlePlay = () => {
  if (!isPlaying.value) {
    monteCarloStore.startSimulation(selectedTimeframe.value, selectedSpeed.value)
  } else if (isPaused.value) {
    monteCarloStore.resumeSimulation()
  }
}

const handlePause = () => {
  monteCarloStore.pauseSimulation()
}

const handleReset = () => {
  monteCarloStore.resetSimulation()
}

const handleTimeframeChange = (years: 5 | 10 | 20) => {
  selectedTimeframe.value = years
  if (isPlaying.value || monteCarloStore.simulationState.isComplete) {
    // Reset simulation but don't auto-start - user must click Play again
    monteCarloStore.resetSimulation()
  }
}

const handleSpeedChange = (speed: 1 | 2 | 5 | 10) => {
  selectedSpeed.value = speed
  if (isPlaying.value && !isPaused.value) {
    monteCarloStore.updateSimulationSpeed(speed)
  }
}

const handleToggleConfidenceBand = () => {
  monteCarloStore.toggleConfidenceBand()
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
  if (!isPlaying.value && !monteCarloStore.simulationState.isComplete) {
    return 'Explore how your portfolio might evolve over time based on historical patterns'
  }

  // Check if simulation is complete
  const isComplete = simulationProgress.value === 100

  // Calculate current year and month
  const currentMonth = monteCarloStore.simulationState.currentIndex
  const currentYear = Math.floor(currentMonth / 12)
  const currentMonthInYear = currentMonth % 12

  // Get current simulated value from median (p50)
  const simulationData = monteCarloStore.simulationPercentiles.p50
  if (simulationData.length > 0 && currentMonth > 0) {
    const dataIndex = Math.min(currentMonth - 1, simulationData.length - 1)
    const currentValue = simulationData[dataIndex]?.value ?? 0
    const startValue = monteCarloStore.historicalData.length > 0
      ? monteCarloStore.historicalData[monteCarloStore.historicalData.length - 1]?.value ?? 0
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
      const finalP25 = monteCarloStore.simulationPercentiles.p25[monteCarloStore.simulationPercentiles.p25.length - 1]?.value ?? 0
      const finalP75 = monteCarloStore.simulationPercentiles.p75[monteCarloStore.simulationPercentiles.p75.length - 1]?.value ?? 0

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
      <button class="info-button" @click="showInfo = !showInfo" aria-label="Toggle information">
        <span class="info-icon">i</span>
      </button>
    </div>

    <!-- Info Panel (Collapsible) -->
    <div v-if="showInfo" class="info-panel">
      <p class="info-text">
        Use these controls to adjust your simulation timeframe, speed, and display options.
      </p>
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

      <!-- Timeframe -->
      <div class="control-section">
        <label class="section-label">Timeframe</label>
        <div class="button-row">
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
     <div class="control-section">
        <label class="section-label">Speed</label>
        <div class="button-row">
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

      <!-- Show Confidence Range Checkbox -->
      <div class="control-section">
        <label class="checkbox-row">
          <input
            type="checkbox"
            :checked="showConfidenceBand"
            @change="handleToggleConfidenceBand"
          />
          <span>Show confidence range</span>
        </label>
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
  gap: 8px;
  padding: 12px;
  background: var(--color-card-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  max-height: 100%;
  overflow: hidden;
  overflow-y: auto;
}

.section-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding-bottom: 8px;
  border-bottom: 1px solid var(--color-border);
  margin-bottom: 2px;
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
  gap: 10px;
}

.controls-row {
  display: flex;
  gap: 8px;
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

.control-section {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.section-label {
  font-size: var(--font-size-xs);
  font-weight: 500;
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.02em;
  cursor: default;
}

.button-row {
  display: flex;
  gap: 6px;
}

.checkbox-row {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  user-select: none;
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.checkbox-row input[type='checkbox'] {
  width: 14px;
  height: 14px;
  cursor: pointer;
  accent-color: var(--color-primary);
  flex-shrink: 0;
}

.checkbox-row:hover {
  color: var(--color-text-primary);
}

.option-buttons {
  display: flex;
  gap: 4px;
}

.btn-option {
  flex: 1;
  padding: 6px 12px;
  border: 1px solid var(--color-border);
  background: transparent;
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  font-weight: 400;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;
  min-width: 0;
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

.info-panel {
  background: rgba(249, 115, 22, 0.03);
  border-left: 2px solid var(--color-primary);
  padding: var(--spacing-sm) var(--spacing-md);
  border-radius: 2px;
}

.info-panel .info-text {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  margin: 0;
  line-height: 1.5;
}

.status-section {
  display: flex;
  flex-direction: column;
  gap: 3px;
  padding-top: 6px;
  border-top: 1px solid var(--color-border);
  margin-top: 6px;
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
  line-height: 1.4;
}
</style>
