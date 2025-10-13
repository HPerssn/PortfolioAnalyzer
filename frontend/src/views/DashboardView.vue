<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { portfolioService } from '@/api/portfolioService'
import type { PortfolioSummary, PortfolioHistoryPoint } from '@/types/portfolio'
import PixelIcon from '@/components/PixelIcon.vue'
import PortfolioInputForm from '@/components/PortfolioInputForm.vue'
import PortfolioSelector from '@/components/PortfolioSelector.vue'
import PortfolioChart from '@/components/PortfolioChart.vue'
import { usePortfolioStore } from '@/stores/portfolioStore'
import { formatCurrency, formatPercentage } from '@/utils/formatters'
import { ASSET_COLORS, DEFAULT_COLOR } from '@/constants/colors'

const portfolioStore = usePortfolioStore()
const portfolio = ref<PortfolioSummary | null>(null)
const portfolioHistory = ref<PortfolioHistoryPoint[]>([])
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

const returnClass = computed(() => {
  if (!portfolio.value) return ''
  return portfolio.value.totalReturn >= 0 ? 'positive' : 'negative'
})

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
      <div class="stats-grid">
        <div class="stat-card">
          <div class="stat-icon">
            <PixelIcon type="dollar" :size="40" />
          </div>
          <div class="stat-info">
            <div class="stat-label">Total Value</div>
            <div class="stat-value">{{ formatCurrency(portfolio.totalValue) }}</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon">
            <PixelIcon type="bar-chart" :size="40" />
          </div>
          <div class="stat-info">
            <div class="stat-label">Day Change</div>
            <div class="stat-value">$0.00</div>
            <div class="stat-change">+0.00%</div>
          </div>
        </div>

        <div class="stat-card" :class="returnClass">
          <div class="stat-icon">
            <PixelIcon type="arrow-up" :size="40" />
          </div>
          <div class="stat-info">
            <div class="stat-label">Total Return</div>
            <div class="stat-value">{{ formatCurrency(portfolio.totalReturn) }}</div>
            <div class="stat-change">{{ formatPercentage(portfolio.totalReturnPercentage) }}</div>
          </div>
        </div>

        <div class="stat-card">
          <div class="stat-icon">
            <PixelIcon type="pie-chart" :size="40" />
          </div>
          <div class="stat-info">
            <div class="stat-label">Holdings</div>
            <div class="stat-value">{{ portfolio.assetCount }}</div>
          </div>
        </div>
      </div>

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
          <!-- Holdings Section -->
          <div class="sidebar-section">
            <h3>Holdings</h3>
            <div class="holdings-list">
              <div v-for="asset in portfolio.assets" :key="asset.symbol" class="holding-item">
                <div class="holding-header">
                  <span class="holding-symbol">{{ asset.symbol }}</span>
                  <span class="holding-value">{{ formatCurrency(asset.currentValue) }}</span>
                </div>
                <div class="holding-details">
                  <span class="holding-quantity">{{ asset.quantity }} shares</span>
                  <span class="holding-return" :class="asset.return >= 0 ? 'positive' : 'negative'">
                    {{ formatPercentage(asset.returnPercentage) }}
                  </span>
                </div>
                <div class="progress-bar">
                  <div
                    class="progress-fill"
                    :style="{ width: `${Math.abs(asset.returnPercentage)}%` }"
                  ></div>
                </div>
              </div>
            </div>
          </div>

          <!-- Allocation Section -->
          <div class="sidebar-section">
            <h3>Allocation</h3>
            <div class="allocation-list">
              <div v-for="asset in portfolio.assets" :key="asset.symbol" class="allocation-item">
                <div
                  class="allocation-color"
                  :style="{ background: getAssetColor(asset.symbol) }"
                ></div>
                <span class="allocation-label">{{ asset.symbol }}</span>
                <span class="allocation-percent"
                  >{{ ((asset.currentValue / portfolio.totalValue) * 100).toFixed(1) }}%</span
                >
              </div>
            </div>
          </div>

          <!-- Activity Section -->
          <div class="sidebar-section">
            <h3>Activity</h3>
            <div class="activity-list">
              <div class="activity-item">
                <span class="activity-type buy">BUY</span>
                <span class="activity-text">AAPL · 10 shares</span>
                <span class="activity-date">Jan 1</span>
              </div>
              <div class="activity-item">
                <span class="activity-type buy">BUY</span>
                <span class="activity-text">GOOGL · 5 shares</span>
                <span class="activity-date">Jan 1</span>
              </div>
              <div class="activity-item">
                <span class="activity-type buy">BUY</span>
                <span class="activity-text">MSFT · 8 shares</span>
                <span class="activity-date">Jan 1</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
export default {
  methods: {
    getAssetColor(symbol: string): string {
      const hash = symbol.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0)
      return ASSET_COLORS[hash % ASSET_COLORS.length] || DEFAULT_COLOR
    },
  },
}
</script>

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

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--spacing-md);
  padding: var(--spacing-lg) var(--spacing-xl);
  background: var(--color-bg-base);
}

.stat-card {
  display: flex;
  gap: var(--spacing-lg);
  background: var(--color-card-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--spacing-lg);
  transition: all 0.2s ease;
}

.stat-card:hover {
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

.stat-icon {
  color: var(--color-text-muted);
  flex-shrink: 0;
}

.stat-info {
  flex: 1;
  min-width: 0;
}

.stat-label {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  font-weight: 400;
  margin-bottom: 2px;
}

.stat-value {
  font-size: var(--font-size-lg);
  font-weight: 300;
  color: var(--color-text-primary);
  letter-spacing: -0.01em;
}

.stat-change {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  font-weight: 400;
  display: inline-block;
  padding: 2px 8px;
  border-radius: 4px;
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

.sidebar-section {
  background: var(--color-card-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--spacing-md) var(--spacing-lg);
}

.sidebar-section h3 {
  font-size: var(--font-size-md);
  font-weight: 400;
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-lg) 0;
  letter-spacing: -0.02em;
  text-transform: uppercase;
  font-size: 11px;
  letter-spacing: 0.08em;
}

.holdings-list,
.allocation-list,
.activity-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
}

.holding-item {
  padding-bottom: var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
}

.holding-item:last-child {
  border-bottom: none;
  padding-bottom: 0;
}

.holding-header {
  display: flex;
  justify-content: space-between;
  margin-bottom: 4px;
}

.holding-symbol {
  font-size: var(--font-size-sm);
  font-weight: 400;
  color: var(--color-text-primary);
  letter-spacing: 0.02em;
}

.holding-value {
  font-size: var(--font-size-sm);
  font-weight: 300;
  color: var(--color-text-primary);
}

.holding-details {
  display: flex;
  justify-content: space-between;
  margin-bottom: 6px;
}

.holding-quantity {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
  font-weight: 300;
}

.holding-return {
  font-size: var(--font-size-xs);
  font-weight: 400;
}

.holding-return.positive {
  color: var(--color-positive);
}

.holding-return.negative {
  color: var(--color-negative);
}

.progress-bar {
  height: 2px;
  background: var(--color-border);
  border-radius: 1px;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: var(--color-primary);
  border-radius: 1px;
  transition: width 0.3s ease;
}

.allocation-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  font-size: var(--font-size-xs);
}

.allocation-color {
  width: 12px;
  height: 12px;
  border-radius: 2px;
}

.allocation-label {
  flex: 1;
  color: var(--color-text-primary);
  font-weight: 400;
  letter-spacing: 0.02em;
}

.allocation-percent {
  color: var(--color-text-muted);
  font-weight: 300;
}

.activity-item {
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-md);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-sm);
  background: var(--color-card-bg);
  transition: all 0.2s;
}

.activity-item:hover {
  border-color: var(--color-border-hover);
  transform: translateX(2px);
  box-shadow: var(--shadow-sm);
}

.activity-type {
  font-size: 9px;
  font-weight: 600;
  letter-spacing: 0.1em;
  padding: 2px 6px;
  border-radius: 3px;
}

.activity-type.buy,
.stat-change {
  background: rgba(16, 163, 74, 0.1);
  color: var(--color-positive);
}

.activity-text {
  flex: 1;
  font-size: var(--font-size-xs);
  color: var(--color-text-primary);
  font-weight: 300;
}

.activity-date {
  font-size: var(--font-size-xs);
  color: var(--color-text-light);
  font-weight: 300;
}

/* --- Responsive Adjustments --- */

/* Tablets (<= 1024px) */
@media (max-width: 1024px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
    gap: var(--spacing-md);
  }

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

  .sidebar-section {
    flex: 1 1 calc(50% - var(--spacing-md));
    min-width: 280px;
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

  .stats-grid {
    grid-template-columns: 1fr;
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

  .sidebar-section {
    width: 100%;
  }

  .stat-card {
    flex-direction: row;
    align-items: center;
  }

  .stat-value {
    font-size: 1.2rem;
  }

  .stat-icon {
    display: none; /* hides icons to preserve space */
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

  .sidebar-section h3 {
    font-size: 10px;
  }

  .stat-card {
    padding: var(--spacing-md);
  }

  .chart-placeholder {
    background-size: 12px 12px;
  }
}
</style>
