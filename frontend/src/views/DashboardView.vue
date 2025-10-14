<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { portfolioService } from '@/api/portfolioService'
import type { PortfolioSummary, PortfolioHistoryPoint, BenchmarkComparison } from '@/types/portfolio'
import PortfolioInputForm from '@/components/PortfolioInputForm.vue'
import PortfolioSelector from '@/components/PortfolioSelector.vue'
import PortfolioChart from '@/components/PortfolioChart.vue'
import StatsGrid from '@/components/StatsGrid.vue'
import HoldingsList from '@/components/HoldingsList.vue'
import AllocationList from '@/components/AllocationList.vue'
import ActivityList from '@/components/ActivityList.vue'
import { usePortfolioStore } from '@/stores/portfolioStore'

const portfolioStore = usePortfolioStore()
const portfolio = ref<PortfolioSummary | null>(null)
const portfolioHistory = ref<PortfolioHistoryPoint[]>([])
const benchmarkComparison = ref<BenchmarkComparison | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const showForm = ref(true)
const inputFormRef = ref<InstanceType<typeof PortfolioInputForm> | null>(null)

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
  } catch (err) {
    console.error('Failed to fetch portfolio history:', err)
    // Don't show error to user, just use placeholder data in chart
    portfolioHistory.value = []
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

// Sample activity data
const activities = [
  { type: 'buy' as const, symbol: 'AAPL', quantity: 10, date: 'Jan 1' },
  { type: 'buy' as const, symbol: 'GOOGL', quantity: 5, date: 'Jan 1' },
  { type: 'buy' as const, symbol: 'MSFT', quantity: 8, date: 'Jan 1' },
]

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
        :asset-count="portfolio.assetCount"
        :benchmark-comparison="benchmarkComparison"
      />

      <!-- Main Content Area -->
      <div class="main-content">
        <!-- Chart Section (Left, 2/3) -->
        <div class="chart-section">
          <div class="section-header">
            <h2>Performance</h2>
            <div class="time-selector">
              <button class="active">1D</button>
              <button>1W</button>
              <button>1M</button>
              <button>3M</button>
              <button>1Y</button>
              <button>ALL</button>
            </div>
          </div>
          <PortfolioChart :history="portfolioHistory" />
        </div>

        <!-- Sidebar (Right, 1/3) -->
        <div class="sidebar">
          <HoldingsList :assets="portfolio.assets" />
          <AllocationList :assets="portfolio.assets" :total-value="portfolio.totalValue" />
          <ActivityList :activities="activities" />
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
  border: 1px solid #e5e5e5;
  border-radius: 9999px; /* pill shape */
  color: #666;
  font-size: var(--font-size-xs);
  font-weight: 400;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-toggle-form:hover {
  border-color: #f97316;
  color: #f97316;
  background: #fff7f0;
}

.btn-toggle-form:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  border-color: #e5e5e5;
  color: #aaa;
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
  display: flex;
  gap: 4px;
}

.time-selector button {
  padding: 4px 12px;
  border: 1px solid var(--color-border);
  background: transparent;
  color: var(--color-text-muted);
  font-size: var(--font-size-xs);
  font-weight: 400;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;
}

.time-selector button:hover {
  border-color: var(--color-border-hover);
}

.time-selector button.active {
  background: var(--color-primary);
  color: white;
  border-color: var(--color-primary);
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
  background: #d4d4d4;
  border-radius: 4px;
}

.sidebar::-webkit-scrollbar-thumb:hover {
  background: #a3a3a3;
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
