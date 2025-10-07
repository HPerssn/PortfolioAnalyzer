<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { portfolioService } from '@/api/portfolioService'
import type { PortfolioSummary } from '@/types/portfolio'
import PixelIcon from '@/components/PixelIcon.vue'
import PortfolioInputForm from '@/components/PortfolioInputForm.vue'

const portfolio = ref<PortfolioSummary | null>(null)
const loading = ref(true)
const error = ref<string | null>(null)
const showForm = ref(true)

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

const handleCalculated = (summary: PortfolioSummary) => {
  portfolio.value = summary
  loading.value = false
  error.value = null
  showForm.value = false
}

const handleError = (message: string) => {
  error.value = message
  setTimeout(() => {
    error.value = null
  }, 5000)
}

const toggleForm = () => {
  showForm.value = !showForm.value
}

const formatCurrency = (value: number) => {
  return new Intl.NumberFormat('sv-SE', {
    style: 'currency',
    currency: 'USD',
  }).format(value)
}

const formatPercentage = (value: number) => {
  return `${value >= 0 ? '+' : ''}${value.toFixed(2)}%`
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

    <div v-if="showForm" class="form-container">
      <PortfolioInputForm @calculated="handleCalculated" @error="handleError" />
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
          <div class="chart-placeholder">
            <div class="chart-grid"></div>
          </div>
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
      const colors = ['#404040', '#525252', '#737373', '#a3a3a3', '#d4d4d4', '#f97316']
      const hash = symbol.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0)
      return colors[hash % colors.length] || '#737373'
    },
  },
}
</script>

<style scoped>
.dashboard {
  height: 100vh;
  display: flex;
  flex-direction: column;
  overflow: hidden;
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
  transition: all 0.25s ease;
}

.btn-toggle-form:hover {
  border-color: #f97316;
  color: #f97316;
  background: #fff7f0;
  transform: translateY(-1px);
}

.btn-toggle-form:active {
  transform: translateY(0);
  box-shadow: none;
}

.btn-toggle-form:disabled {
  opacity: 0.6;
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

.chart-placeholder {
  flex: 1;
  position: relative;
  background: #fafafa;
  background-image: radial-gradient(circle, #c4c4c4 1px, transparent 1px);
  background-size: 20px 20px;
}

.chart-grid {
  width: 100%;
  height: 100%;
  background-image:
    linear-gradient(to right, var(--color-border) 1px, transparent 1px),
    linear-gradient(to bottom, var(--color-border) 1px, transparent 1px);
  background-size: 50px 50px;
  opacity: 0.3;
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
</style>
