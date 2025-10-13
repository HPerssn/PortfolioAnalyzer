<script setup lang="ts">
import { ref, computed } from 'vue'
import { portfolioService, type HoldingInput } from '@/api/portfolioService'
import type { PortfolioSummary } from '@/types/portfolio'
import {
  isValidTickerSymbol,
  isValidQuantity,
  isValidPurchaseDate,
  getTickerErrorMessage,
  getQuantityErrorMessage,
} from '@/utils/validation'
import { usePortfolioStore } from '@/stores/portfolioStore'

const emit = defineEmits<{
  calculated: [
    summary: PortfolioSummary,
    holdings: Array<{ symbol: string; quantity: number }>,
    purchaseDate: string,
  ]
  error: [message: string]
  saved: []
}>()

const portfolioStore = usePortfolioStore()

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
const showSaveModal = ref(false)
const portfolioName = ref('')
const saving = ref(false)
const loadedPortfolioId = ref<number | null>(null) // Track which portfolio is loaded
const loadedPortfolioName = ref<string | null>(null) // Track original name

// Expose method to load portfolio data from parent component
const loadPortfolioData = (
  portfolioId: number,
  portfolioNameValue: string,
  loadedHoldings: Array<{ symbol: string; quantity: number }>,
  loadedPurchaseDate: string,
) => {
  loadedPortfolioId.value = portfolioId
  portfolioName.value = portfolioNameValue
  loadedPortfolioName.value = portfolioNameValue // Store original name
  holdings.value = loadedHoldings.map((h) => ({
    symbol: h.symbol,
    quantity: h.quantity,
  }))
  purchaseDate.value = loadedPurchaseDate
}

// Clear loaded portfolio and reset to new portfolio mode
const clearLoadedPortfolio = () => {
  loadedPortfolioId.value = null
  loadedPortfolioName.value = null
  portfolioName.value = ''
  holdings.value = [
    { symbol: 'AAPL', quantity: 10 },
    { symbol: 'GOOGL', quantity: 5 },
    { symbol: 'MSFT', quantity: 8 },
  ]
  purchaseDate.value = '2024-01-01'
}

// Expose the methods to parent via defineExpose
defineExpose({
  loadPortfolioData,
  clearLoadedPortfolio,
})

const addHolding = () => {
  holdings.value.push({ symbol: '', quantity: 0 })
}

const removeHolding = (index: number) => {
  holdings.value.splice(index, 1)
}

// Validation helpers
const isSymbolValid = (symbol: string) => isValidTickerSymbol(symbol)
const isQuantityValid = (quantity: number) => isValidQuantity(quantity)

// Check if form has any validation errors
const hasValidationErrors = computed(() => {
  return holdings.value.some(
    (h) => h.symbol.trim() !== '' && (!isSymbolValid(h.symbol) || !isQuantityValid(h.quantity)),
  )
})

// Check if we're editing an existing portfolio
const isEditing = computed(() => loadedPortfolioId.value !== null)

// Check if name has changed (means save as new, not update)
const nameHasChanged = computed(() => {
  if (!loadedPortfolioName.value) return false
  return portfolioName.value.trim() !== loadedPortfolioName.value.trim()
})

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

  if (!isValidPurchaseDate(purchaseDate.value)) {
    emit('error', 'Please select a valid purchase date (must be in the past)')
    return
  }

  // Check for validation errors
  const errors: string[] = []
  validHoldings.forEach((h) => {
    if (!isSymbolValid(h.symbol)) {
      errors.push(getTickerErrorMessage(h.symbol))
    }
    if (!isQuantityValid(h.quantity)) {
      errors.push(`${h.symbol}: ${getQuantityErrorMessage(h.quantity)}`)
    }
  })

  if (errors.length > 0) {
    emit('error', errors.join('; '))
    return
  }

  try {
    loading.value = true
    const holdingsToSubmit = validHoldings.map((h) => ({
      symbol: h.symbol.toUpperCase(),
      quantity: h.quantity,
    }))
    const result = await portfolioService.calculatePortfolio({
      holdings: holdingsToSubmit,
      purchaseDate: purchaseDate.value,
    })
    emit('calculated', result, holdingsToSubmit, purchaseDate.value)
  } catch (err: any) {
    // Extract error message from API response
    const errorMessage =
      err?.response?.data?.error || err.message || 'Failed to calculate portfolio'
    const errorDetails = err?.response?.data?.details

    if (errorDetails && Array.isArray(errorDetails)) {
      emit('error', `${errorMessage}: ${errorDetails.join('; ')}`)
    } else {
      emit('error', errorMessage)
    }
  } finally {
    loading.value = false
  }
}

const openSaveModal = () => {
  showSaveModal.value = true
  // Keep current name if editing, clear if creating new
  if (!loadedPortfolioId.value) {
    portfolioName.value = ''
  }
}

const closeSaveModal = () => {
  showSaveModal.value = false
}

const savePortfolio = async () => {
  if (!portfolioName.value.trim()) {
    emit('error', 'Please enter a portfolio name')
    return
  }

  const validHoldings = holdings.value.filter((h) => h.symbol.trim() !== '' && h.quantity > 0)

  if (validHoldings.length === 0) {
    emit('error', 'Please add at least one holding before saving')
    return
  }

  try {
    saving.value = true

    // If name has changed, always save as new portfolio (clone)
    // Otherwise, update if we have an ID, or create new if we don't
    if (loadedPortfolioId.value && !nameHasChanged.value) {
      // Update existing portfolio (name unchanged)
      await portfolioStore.updatePortfolio(
        loadedPortfolioId.value,
        portfolioName.value.trim(),
        purchaseDate.value,
        validHoldings.map((h) => ({
          symbol: h.symbol.toUpperCase(),
          quantity: h.quantity,
        })),
      )
    } else {
      // Create new portfolio (either no ID, or name changed = clone)
      await portfolioStore.savePortfolio(
        portfolioName.value.trim(),
        purchaseDate.value,
        validHoldings.map((h) => ({
          symbol: h.symbol.toUpperCase(),
          quantity: h.quantity,
        })),
      )

      // After creating new, clear the loaded portfolio state
      loadedPortfolioId.value = null
      loadedPortfolioName.value = null
    }

    closeSaveModal()
    emit('saved')

    // Automatically calculate portfolio after saving
    await calculatePortfolio()
  } catch (err: any) {
    emit('error', err.message || 'Failed to save portfolio')
  } finally {
    saving.value = false
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
            :class="{
              'input-error': holding.symbol.trim() !== '' && !isSymbolValid(holding.symbol),
            }"
            maxlength="20"
            @input="holding.symbol = holding.symbol.toUpperCase()"
          />
          <input
            v-model.number="holding.quantity"
            type="number"
            placeholder="10"
            class="input quantity-input"
            :class="{
              'input-error':
                holding.quantity != null &&
                holding.quantity !== 0 &&
                !isQuantityValid(holding.quantity),
            }"
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

      <div class="button-group">
        <button
          @click="calculatePortfolio"
          :disabled="loading || hasValidationErrors"
          class="btn-calculate"
        >
          {{ loading ? 'Calculating...' : 'Calculate Portfolio' }}
        </button>
        <button @click="openSaveModal" :disabled="hasValidationErrors" class="btn-save">
          Save Portfolio
        </button>
      </div>
    </div>

    <!-- Save Portfolio Modal -->
    <div v-if="showSaveModal" class="modal-overlay" @click.self="closeSaveModal">
      <div class="modal">
        <div class="modal-header">
          <h3>
            {{
              isEditing && !nameHasChanged ? 'Update Portfolio' : 'Save Portfolio'
            }}
          </h3>
          <button @click="closeSaveModal" class="modal-close">×</button>
        </div>
        <div class="modal-body">
          <label for="portfolio-name" class="label">Portfolio Name</label>
          <input
            id="portfolio-name"
            v-model="portfolioName"
            type="text"
            class="input"
            placeholder="My Tech Portfolio"
            @keyup.enter="savePortfolio"
            autofocus
          />
          <p v-if="nameHasChanged" class="name-changed-hint">
            💡 Name changed - this will save as a new portfolio
          </p>
        </div>
        <div class="modal-footer">
          <button @click="closeSaveModal" class="btn-cancel">Cancel</button>
          <button
            @click="savePortfolio"
            :disabled="!portfolioName.trim() || saving"
            class="btn-confirm"
          >
            {{
              saving
                ? isEditing && !nameHasChanged
                  ? 'Updating...'
                  : 'Saving...'
                : isEditing && !nameHasChanged
                  ? 'Update'
                  : 'Save as New'
            }}
          </button>
        </div>
      </div>
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

.input-error {
  border-color: #ef4444; /* red */
  background: #fef2f2; /* light red background */
}

.input-error:focus {
  border-color: #dc2626; /* darker red */
  box-shadow: 0 0 0 2px rgba(239, 68, 68, 0.1);
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

/* ---------- BUTTON GROUP ---------- */

.button-group {
  display: flex;
  gap: var(--spacing-sm);
}

.btn-calculate,
.btn-save {
  flex: 1;
  padding: 0.5rem 1.2rem;
  background: white;
  border: 1px solid #e5e5e5;
  border-radius: 6px;
  color: #666;
  font-size: var(--font-size-sm);
  font-weight: 400;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-calculate:hover:not(:disabled),
.btn-save:hover:not(:disabled) {
  border-color: #f97316;
  color: #f97316;
  background: #fff7f0;
}

.btn-calculate:disabled,
.btn-save:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  border-color: #e5e5e5;
  color: #aaa;
  background: white;
}

/* ---------- MODAL ---------- */

.modal-overlay {
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
}

.modal {
  background: white;
  border-radius: 8px;
  width: 90%;
  max-width: 400px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: var(--spacing-lg);
  border-bottom: 1px solid #e5e5e5;
}

.modal-header h3 {
  margin: 0;
  font-size: var(--font-size-md);
  font-weight: 400;
  color: var(--color-text-primary);
}

.modal-close {
  background: transparent;
  border: none;
  font-size: 24px;
  color: #999;
  cursor: pointer;
  padding: 0;
  width: 32px;
  height: 32px;
  transition: color 0.2s;
}

.modal-close:hover {
  color: #333;
}

.modal-body {
  padding: var(--spacing-lg);
  display: flex;
  flex-direction: column;
  gap: var(--spacing-xs);
}

.name-changed-hint {
  margin: var(--spacing-xs) 0 0 0;
  padding: var(--spacing-xs) var(--spacing-sm);
  background: #fff7ed;
  border: 1px solid #fed7aa;
  border-radius: 4px;
  color: #c2410c;
  font-size: var(--font-size-xs);
  font-weight: 400;
}

.modal-footer {
  display: flex;
  gap: var(--spacing-sm);
  padding: var(--spacing-lg);
  border-top: 1px solid #e5e5e5;
  justify-content: flex-end;
}

.btn-cancel,
.btn-confirm {
  padding: 0.5rem 1.2rem;
  background: white;
  border: 1px solid #e5e5e5;
  border-radius: 6px;
  color: #666;
  font-size: var(--font-size-sm);
  font-weight: 400;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-cancel:hover {
  border-color: #999;
  color: #333;
}

.btn-confirm:hover:not(:disabled) {
  border-color: #f97316;
  color: #f97316;
  background: #fff7f0;
}

.btn-confirm:disabled {
  opacity: 0.5;
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
