<script setup lang="ts">
import { ref } from 'vue'
import { portfolioService, type HoldingInput } from '@/api/portfolioService'
import type { PortfolioSummary } from '@/types/portfolio'

const emit = defineEmits<{
  calculated: [summary: PortfolioSummary]
  error: [message: string]
}>()

interface Holding {
  symbol: string
  quantity: number
}

const holdings = ref<Holding[]>([
  { symbol: 'AAPL', quantity: 10 },
  { symbol: 'GOOGL', quantity: 5 },
  { symbol: 'MSFT', quantity: 8 },
])

const purchaseDate = ref('2024-01-01')
const loading = ref(false)

const addHolding = () => {
  holdings.value.push({ symbol: '', quantity: 0 })
}

const removeHolding = (index: number) => {
  holdings.value.splice(index, 1)
}

const calculatePortfolio = async () => {
  // Validate
  const validHoldings = holdings.value.filter((h) => h.symbol.trim() !== '' && h.quantity > 0)

  if (validHoldings.length === 0) {
    emit('error', 'Please add at least one holding with a valid symbol and quantity')
    return
  }

  if (!purchaseDate.value) {
    emit('error', 'Please select a purchase date')
    return
  }

  try {
    loading.value = true
    const result = await portfolioService.calculatePortfolio({
      holdings: validHoldings.map((h) => ({
        symbol: h.symbol.toUpperCase(),
        quantity: h.quantity,
      })),
      purchaseDate: purchaseDate.value,
    })
    emit('calculated', result)
  } catch (err) {
    emit('error', err instanceof Error ? err.message : 'Failed to calculate portfolio')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="portfolio-form">
    <div class="form-header">
      <h2>Your Portfolio</h2>
      <p class="subtitle">Add your holdings to see live performance data</p>
    </div>

    <div class="form-content">
      <div class="holdings-section">
        <div class="holdings-header">
          <span class="label">Symbol</span>
          <span class="label">Quantity</span>
          <span class="label-action"></span>
        </div>

        <div v-for="(holding, index) in holdings" :key="index" class="holding-row">
          <input
            v-model="holding.symbol"
            type="text"
            placeholder="AAPL"
            class="input symbol-input"
            maxlength="10"
          />
          <input
            v-model.number="holding.quantity"
            type="number"
            placeholder="10"
            class="input quantity-input"
            min="0"
            step="0.01"
          />
          <button
            @click="removeHolding(index)"
            class="btn-remove"
            :disabled="holdings.length === 1"
            title="Remove holding"
          >
            ×
          </button>
        </div>

        <button @click="addHolding" class="btn-add">+ Add Holding</button>
      </div>

      <div class="date-section">
        <label for="purchase-date" class="label">Purchase Date</label>
        <input
          id="purchase-date"
          v-model="purchaseDate"
          type="date"
          class="input date-input"
          max="2099-12-31"
        />
      </div>

      <button @click="calculatePortfolio" :disabled="loading" class="btn-calculate">
        {{ loading ? 'Calculating...' : 'Calculate Portfolio' }}
      </button>
    </div>
  </div>
</template>

<style scoped>
.portfolio-form {
  background: var(--color-card-bg);
  border: 1px solid #f3f3f3;
  border-radius: var(--radius-md);
  padding: var(--spacing-xl);
}

/* ---------- HEADER ---------- */

.form-header {
  margin-bottom: var(--spacing-xl);
  border-bottom: 1px solid transparent;
  position: relative;
  padding-bottom: var(--spacing-sm);
}

.form-header h2 {
  font-size: var(--font-size-lg);
  font-weight: 400;
  color: var(--color-text-primary);
  margin: 0 0 var(--spacing-xs) 0;
  letter-spacing: -0.02em;
}

.form-header::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  width: 60px;
  height: 2px;
  background: var(--color-accent, #f97316); /* thin orange underline */
  border-radius: 1px;
}

.subtitle {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  margin: 0;
  font-weight: 300;
}

/* ---------- FORM CONTENT ---------- */

.form-content {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-lg);
}

/* ---------- INPUT FIELDS ---------- */

.input {
  width: 100%;
  padding: 0.6rem 0.8rem;
  background: white;
  border: 1px solid #e5e5e5;
  border-radius: 6px;
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  font-weight: 300;
  transition:
    border-color 0.2s,
    box-shadow 0.2s;
  appearance: none;
}

.input:focus {
  outline: none;
  border-color: #f97316; /* orange accent */
  box-shadow: 0 0 0 2px rgba(249, 115, 22, 0.1);
}

.input::placeholder {
  color: #999;
}

.symbol-input {
  text-transform: uppercase;
}

.quantity-input {
  text-align: right;
}

.date-input {
  max-width: 200px;
  appearance: none;
  background-color: white;
  color-scheme: light;
}

/* Remove default date picker styling */
.date-input::-webkit-calendar-picker-indicator {
  filter: invert(50%);
  cursor: pointer;
  opacity: 0.5;
  transition: opacity 0.2s;
}
.date-input:hover::-webkit-calendar-picker-indicator {
  opacity: 0.8;
}

/* ---------- HOLDINGS ---------- */

.holdings-section {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-sm);
}

.holdings-header {
  display: grid;
  grid-template-columns: 2fr 1fr 40px;
  gap: var(--spacing-sm);
  padding: 0 var(--spacing-xs);
}

.label {
  font-size: var(--font-size-xs);
  font-weight: 400;
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.holding-row {
  display: grid;
  grid-template-columns: 2fr 1fr 40px;
  gap: var(--spacing-sm);
  align-items: center;
}

/* ---------- REMOVE BUTTON ---------- */

.btn-remove {
  width: 40px;
  height: 40px;
  padding: 0;
  background: transparent;
  border: none;
  color: #aaa;
  font-size: 22px;
  cursor: pointer;
  line-height: 1;
  transition:
    color 0.2s,
    transform 0.2s;
}

.btn-remove:hover:not(:disabled) {
  color: #f97316;
  transform: scale(1.1);
}

.btn-remove:disabled {
  opacity: 0.3;
  cursor: not-allowed;
}

/* ---------- ADD BUTTON ---------- */

.btn-add {
  padding: var(--spacing-sm) var(--spacing-md);
  background: white;
  border: 1px dashed #e5e5e5;
  border-radius: 6px;
  color: #999;
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: all 0.2s;
}

.btn-add:hover {
  border-color: #f97316;
  color: #f97316;
  background: rgba(249, 115, 22, 0.03);
}

/* ---------- DATE SECTION ---------- */

.date-section {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

/* ---------- CALCULATE BUTTON ---------- */

.btn-calculate {
  padding: var(--spacing-md) var(--spacing-xl);
  background: white;
  border: 1px solid #f97316;
  border-radius: 6px;
  color: #f97316;
  font-size: var(--font-size-sm);
  font-weight: 400;
  cursor: pointer;
  transition: all 0.25s ease;
}

.btn-calculate:hover:not(:disabled) {
  background: #fff7f0;
  transform: translateY(-1px);
  box-shadow: 0 2px 6px rgba(249, 115, 22, 0.08);
}

.btn-calculate:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  border-color: #e5e5e5;
  color: #aaa;
}

/* ---------- RESPONSIVE ---------- */

@media (max-width: 768px) {
  .holding-row,
  .holdings-header {
    grid-template-columns: 1fr 80px 40px;
  }
}
</style>
