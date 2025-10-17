<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import { portfolioService } from '@/api/portfolioService'
import type { PortfolioSummary, PortfolioHistoryPoint, BenchmarkComparison } from '@/types/portfolio'
import PortfolioInputForm from '@/components/PortfolioInputForm.vue'
import PortfolioSelector from '@/components/PortfolioSelector.vue'
import PortfolioChart from '@/components/PortfolioChart.vue'
import StatsGrid from '@/components/StatsGrid.vue'
import SimulationControls from '@/components/SimulationControls.vue'
import { usePortfolioStore } from '@/stores/portfolioStore'
import { useMonteCarloStore } from '@/stores/monteCarloStore'

const portfolioStore = usePortfolioStore()
const monteCarloStore = useMonteCarloStore()
const portfolio = ref<PortfolioSummary | null>(null)
const portfolioHistory = ref<PortfolioHistoryPoint[]>([])
const benchmarkComparison = ref<BenchmarkComparison | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const showForm = ref(true)
const inputFormRef = ref<InstanceType<typeof PortfolioInputForm> | null>(null)
const selectedTimeframe = ref<'1M' | '3M' | '1Y' | '5Y' | 'All'>('1Y')

const fetchPortfolio = async () => {
  try {
    loading.value = true
    error.value = null
    portfolio.value = await portfolioService.getPortfolioSummary()
  } catch (err) {
    error.value = err instanceof Error ? err.message : 'Failed to load portfolio'
    console.error('Error fetching portfolio:', err)
  } finally {
    loading.value = false
  }
}

const handleCalculated = async (
  summary: PortfolioSummary,
  holdings: Array<{ symbol: string; quantity: number }>,
  purchaseDate: string,
) => {
  portfolio.value = summary
  loading.value = false
  error.value = null
  showForm.value = false

  // Fetch portfolio history for the chart
  try {
    const historyResponse = await portfolioService.getPortfolioHistory({
      holdings,
      purchaseDate,
    })
    portfolioHistory.value = historyResponse.history

    // Filter history based on selected timeframe for initial load
    const now = new Date()
    const cutoffDate = new Date(now)
    cutoffDate.setFullYear(now.getFullYear() - 1) // Default 1Y

    const filtered = historyResponse.history.filter(
      (point) => new Date(point.date) >= cutoffDate,
    )

    // Set filtered historical data in store for simulation
    monteCarloStore.setHistoricalData(filtered)
  } catch (err) {
    console.error('Failed to fetch portfolio history:', err)
    // Don't show error to user, just use placeholder data in chart
    portfolioHistory.value = []
    monteCarloStore.setHistoricalData([])
  }

  // Fetch benchmark comparison
  try {
    const benchmarkResponse = await portfolioService.getBenchmarkComparison({
      holdings,
      purchaseDate,
    })
    benchmarkComparison.value = benchmarkResponse
  } catch (err) {
    console.error('Failed to fetch benchmark comparison:', err)
    // Don't show error to user, benchmark card will show placeholder
    benchmarkComparison.value = null
  }
}

const handleError = (message: string) => {
  error.value = message
  setTimeout(() => {
    error.value = null
  }, 5000)
}

const handleSaved = async () => {
  // Refresh the portfolios list
  await portfolioStore.fetchPortfolios()
  // Show success message
  error.value = '✓ Portfolio saved successfully!'
  setTimeout(() => {
    error.value = null
  }, 3000)
}

const handlePortfolioLoaded = (
  portfolioId: number,
  name: string,
  holdings: Array<{ symbol: string; quantity: number }>,
  purchaseDate: string,
) => {
  // Update the form with loaded portfolio data
  if (inputFormRef.value) {
    inputFormRef.value.loadPortfolioData(portfolioId, name, holdings, purchaseDate)
  }
  showForm.value = true
}

const handleNewPortfolio = () => {
  // Clear the form to create a new portfolio
  if (inputFormRef.value) {
    inputFormRef.value.clearLoadedPortfolio()
  }
  showForm.value = true
}

const toggleForm = () => {
  showForm.value = !showForm.value
}

// Filter history based on selected timeframe
const filteredHistory = computed(() => {
  if (!portfolioHistory.value || portfolioHistory.value.length === 0) {
    return []
  }

  const now = new Date()
  let cutoffDate: Date

  switch (selectedTimeframe.value) {
    case '1M':
      cutoffDate = new Date(now)
      cutoffDate.setMonth(now.getMonth() - 1)
      break
    case '3M':
      cutoffDate = new Date(now)
      cutoffDate.setMonth(now.getMonth() - 3)
      break
    case '1Y':
      cutoffDate = new Date(now)
      cutoffDate.setFullYear(now.getFullYear() - 1)
      break
    case '5Y':
      cutoffDate = new Date(now)
      cutoffDate.setFullYear(now.getFullYear() - 5)
      break
    case 'All':
      // Return all available data
      return portfolioHistory.value
    default:
      return portfolioHistory.value
  }

  return portfolioHistory.value.filter((point) => new Date(point.date) >= cutoffDate)
})

// Watcher to update historical data in store when filtered history changes
watch(filteredHistory, (newFiltered) => {
  monteCarloStore.setHistoricalData(newFiltered)
})

const handleTimeframeChange = (event: Event) => {
  const target = event.target as HTMLSelectElement
  selectedTimeframe.value = target.value as '1M' | '3M' | '1Y' | '5Y' | 'All'
}

onMounted(() => {
  fetchPortfolio()
})
</script>

<template>
  <div class="dashboard">
    <!-- Header -->
    <header class="header">
      <div class="header-title">
        <h1>Portfolio</h1>
        <div class="accent-line"></div>
      </div>
      <div class="header-actions">
        <PortfolioSelector
          @portfolio-loaded="handlePortfolioLoaded"
          @new-portfolio="handleNewPortfolio"
        />
        <button @click="toggleForm" class="btn-toggle-form">
          {{ showForm ? 'Hide Form' : 'Edit Portfolio' }}
        </button>
        <div class="header-info" v-if="portfolio">
          Updated
          {{
            new Date(portfolio.lastUpdated).toLocaleTimeString('sv-SE', {
              hour: '2-digit',
              minute: '2-digit',
            })
          }}
        </div>
      </div>
    </header>

    <div v-show="showForm" class="form-container">
      <PortfolioInputForm
        ref="inputFormRef"
        @calculated="handleCalculated"
        @error="handleError"
        @saved="handleSaved"
      />
    </div>

    <div v-if="error" class="error-message">
      <p>{{ error }}</p>
      <button @click="error = null" class="btn-dismiss">Dismiss</button>
    </div>

    <div v-else-if="loading" class="loading">
      <div class="spinner"></div>
      <p>Loading...</p>
    </div>

    <div v-else-if="portfolio" class="dashboard-content">
      <!-- Stats Cards -->
      <StatsGrid
        :total-value="portfolio.totalValue"
        :total-return="portfolio.totalReturn"
        :total-return-percentage="portfolio.totalReturnPercentage"
        :assets="portfolio.assets"
        :benchmark-comparison="benchmarkComparison"
      />

      <!-- Main Content Area -->
      <div class="main-content">
        <!-- Chart Section (Left, 2/3) -->
        <div class="chart-section">
          <div class="section-header">
            <h2>Performance</h2>
            <select
              v-model="selectedTimeframe"
              @change="handleTimeframeChange"
              class="time-selector"
            >
              <option value="1M">1 Month</option>
              <option value="3M">3 Months</option>
              <option value="1Y">1 Year</option>
              <option value="5Y">5 Years</option>
              <option value="All">All</option>
            </select>
          </div>
          <PortfolioChart :history="filteredHistory" :timeframe="selectedTimeframe" :key="selectedTimeframe" />
        </div>

        <!-- Sidebar (Right, 1/3) -->
        <div class="sidebar">
          <SimulationControls />
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.dashboard {
  display: flex;
  flex-direction: column;
  min-height: 100vh; /* base */
}

/* Desktop and large screens: fixed full height, no scroll */
@media (min-width: 1024px) {
  .dashboard {
    height: 100vh;
    overflow: hidden;
  }
}

/* Tablets and smaller: allow scroll */
@media (max-width: 1023px) {
  .dashboard {
    height: auto;
    overflow-y: auto;
  }
}

/* Mobile and tablets — allow scroll */
@media (max-width: 1023px) {
  .main-container {
    height: auto;
    min-height: 100vh;
    overflow-y: auto;
  }
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-lg) var(--spacing-xl);
  border-bottom: 1px solid var(--color-border);
  background: var(--color-card-bg);
}

.header-title h1 {
  font-size: 1.5rem;
  font-weight: 300;
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-xs) 0;
  letter-spacing: -0.02em;
}

.accent-line {
  width: 80px;
  height: 1px;
  background: var(--color-primary);
}

.header-actions {
  display: flex;
  align-items: center;
  gap: var(--spacing-lg);
}

.header-info {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  font-weight: 300;
}

.btn-toggle-form {
  padding: 0.35rem 0.9rem;
  background: white;
  border: 1px solid var(--color-border);
  border-radius: 9999px; /* pill shape */
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  font-weight: 400;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-toggle-form:hover {
  border-color: var(--color-primary);
  color: var(--color-primary);
  background: #fff7f0;
}

.btn-toggle-form:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  border-color: var(--color-border);
  color: var(--color-text-light);
}

.form-container {
  padding: var(--spacing-lg) var(--spacing-xl);
  background: var(--color-bg-base);
}

.error-message,
.loading {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  flex: 1;
  color: var(--color-text-muted);
  font-weight: 300;
}

.spinner {
  width: 32px;
  height: 32px;
  border: 2px solid var(--color-border);
  border-top: 2px solid var(--color-primary);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: var(--spacing-md);
}

@keyframes spin {
  to {
    transform: rotate(360deg);
  }
}

.dashboard-content {
  display: flex;
  flex-direction: column;
  flex: 1;
  overflow: hidden;
}

.main-content {
  display: grid;
  grid-template-columns: 2fr 1fr;
  gap: var(--spacing-lg);
  padding: 0 var(--spacing-xl) var(--spacing-xl);
  flex: 1;
  overflow: hidden;
}

.chart-section {
  display: flex;
  flex-direction: column;
  background: var(--color-card-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  overflow: hidden;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-md) var(--spacing-lg);
  border-bottom: 1px solid var(--color-border);
}

.section-header h2 {
  font-size: var(--font-size-md);
  font-weight: 300;
  color: var(--color-text-primary);
  margin: 0;
}

.time-selector {
  padding: 4px 12px;
  border: 1px solid var(--color-border);
  background: white;
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  font-weight: 400;
  border-radius: 6px;
  cursor: pointer;
  transition: all 0.2s;
  appearance: none;
  background-image: url("data:image/svg+xml,%3Csvg width='12' height='8' viewBox='0 0 12 8' fill='none' xmlns='http://www.w3.org/2000/svg'%3E%3Cpath d='M1 1.5L6 6.5L11 1.5' stroke='%23737373' stroke-width='1.5' stroke-linecap='round' stroke-linejoin='round'/%3E%3C/svg%3E");
  background-repeat: no-repeat;
  background-position: right 8px center;
  padding-right: 32px;
}

.time-selector:hover {
  border-color: var(--color-primary);
}

.time-selector:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 2px rgba(249, 115, 22, 0.1);
}

.sidebar {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xl);
  overflow-y: auto;
  padding-right: 8px;
}

.sidebar::-webkit-scrollbar {
  width: 8px;
}

.sidebar::-webkit-scrollbar-track {
  background: transparent;
}

.sidebar::-webkit-scrollbar-thumb {
  background: var(--color-border-hover);
  border-radius: 4px;
}

.sidebar::-webkit-scrollbar-thumb:hover {
  background: var(--color-text-light);
}

/* --- Responsive Adjustments --- */

/* Tablets (<= 1024px) */
@media (max-width: 1024px) {
  .main-content {
    grid-template-columns: 1fr;
  }

  .chart-section {
    order: 1;
  }

  .sidebar {
    order: 2;
    flex-direction: row;
    flex-wrap: wrap;
    gap: var(--spacing-md);
  }
}

/* Mobile (<= 768px) */
@media (max-width: 768px) {
  .header {
    flex-direction: column;
    align-items: flex-start;
    gap: var(--spacing-md);
  }

  .header-actions {
    width: 100%;
    justify-content: space-between;
  }

  .main-content {
    grid-template-columns: 1fr;
    padding: var(--spacing-lg);
  }

  .chart-section {
    height: 240px;
  }

  .sidebar {
    flex-direction: column;
    gap: var(--spacing-md);
    padding-right: 0;
  }

  .sidebar > * {
    width: 100%;
  }
}

/* Very small devices (<= 480px) */
@media (max-width: 480px) {
  .header-title h1 {
    font-size: 1.2rem;
  }

  .btn-toggle-form {
    padding: 0.3rem 0.7rem;
    font-size: 0.75rem;
  }

}
</style>
