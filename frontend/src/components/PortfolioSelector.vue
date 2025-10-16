<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { usePortfolioStore } from '@/stores/portfolioStore'

const emit = defineEmits<{
  portfolioLoaded: [
    portfolioId: number,
    name: string,
    holdings: Array<{ symbol: string; quantity: number }>,
    purchaseDate: string,
  ]
  newPortfolio: []
}>()

const portfolioStore = usePortfolioStore()
const showDropdown = ref(false)
const showDeleteConfirm = ref<number | null>(null)

onMounted(async () => {
  await portfolioStore.fetchPortfolios()
  portfolioStore.loadLastSelectedPortfolio()

  // Load the last selected portfolio if exists
  if (portfolioStore.selectedPortfolioId) {
    await loadPortfolio(portfolioStore.selectedPortfolioId)
  }
})

const toggleDropdown = () => {
  showDropdown.value = !showDropdown.value
  showDeleteConfirm.value = null
}

const createNewPortfolio = () => {
  portfolioStore.selectPortfolio(null)
  emit('newPortfolio')
  showDropdown.value = false
}

const loadPortfolio = async (id: number) => {
  const portfolio = await portfolioStore.fetchPortfolioById(id)
  if (portfolio) {
    portfolioStore.selectPortfolio(id)
    emit(
      'portfolioLoaded',
      portfolio.id,
      portfolio.name,
      portfolio.holdings,
      portfolio.purchaseDate.split('T')[0],
    )
  }
  showDropdown.value = false
}

const confirmDelete = (id: number, event: Event) => {
  event.stopPropagation()
  showDeleteConfirm.value = id
}

const cancelDelete = (event: Event) => {
  event.stopPropagation()
  showDeleteConfirm.value = null
}

const deletePortfolio = async (id: number, event: Event) => {
  event.stopPropagation()
  await portfolioStore.deletePortfolio(id)
  showDeleteConfirm.value = null
}
</script>

<template>
  <div class="portfolio-selector">
    <button @click="toggleDropdown" class="selector-button">
      <span class="selector-label">
        {{
          portfolioStore.selectedPortfolio
            ? portfolioStore.selectedPortfolio.name
            : 'Load Portfolio'
        }}
      </span>
      <span class="selector-arrow">{{ showDropdown ? '▲' : '▼' }}</span>
    </button>

    <div v-if="showDropdown" class="dropdown" @click.stop>
      <button @click="createNewPortfolio" class="new-portfolio-btn">
        <span class="plus-icon">+</span>
        New Portfolio
      </button>
      <div v-if="portfolioStore.savedPortfolios.length === 0" class="dropdown-empty">
        No saved portfolios
      </div>
      <div v-else class="dropdown-list">
        <div
          v-for="portfolio in portfolioStore.savedPortfolios"
          :key="portfolio.id"
          class="dropdown-item"
          :class="{ active: portfolio.id === portfolioStore.selectedPortfolioId }"
        >
          <div class="item-content" @click="loadPortfolio(portfolio.id)">
            <div class="item-name">{{ portfolio.name }}</div>
            <div class="item-meta">
              {{ portfolio.holdings.length }} holdings •
              {{ new Date(portfolio.createdDate).toLocaleDateString('sv-SE') }}
            </div>
          </div>
          <div class="item-actions">
            <button
              v-if="showDeleteConfirm !== portfolio.id"
              @click="confirmDelete(portfolio.id, $event)"
              class="btn-delete"
              title="Delete portfolio"
            >
              ×
            </button>
            <div v-else class="confirm-delete">
              <button @click="deletePortfolio(portfolio.id, $event)" class="btn-confirm-delete">
                Delete?
              </button>
              <button @click="cancelDelete($event)" class="btn-cancel-delete">Cancel</button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.portfolio-selector {
  position: relative;
}

.selector-button {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  background: white;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  cursor: pointer;
  transition: all 0.2s;
}

.selector-button:hover {
  border-color: var(--color-primary);
}

.selector-label {
  font-weight: 400;
}

.selector-arrow {
  font-size: 10px;
  color: var(--color-text-muted);
}

.dropdown {
  position: absolute;
  top: calc(100% + 0.5rem);
  right: 0;
  min-width: 300px;
  background: white;
  border: 1px solid var(--color-border);
  border-radius: 6px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
  z-index: 100;
  max-height: 400px;
  overflow-y: auto;
}

@media (max-width: 640px) {
  .dropdown {
    left: 0;
    right: auto;
    min-width: min(300px, 100vw - 2rem);
    max-width: calc(100vw - 2rem);
  }
}

.new-portfolio-btn {
  width: 100%;
  display: flex;
  align-items: center;
  gap: var(--spacing-sm);
  padding: var(--spacing-md);
  background: white;
  border: none;
  border-bottom: 1px solid var(--color-border);
  color: var(--color-primary);
  font-size: var(--font-size-sm);
  font-weight: 400;
  cursor: pointer;
  transition: all 0.2s;
  text-align: left;
}

.new-portfolio-btn:hover {
  background: #fff7f0;
}

.plus-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 20px;
  height: 20px;
  border-radius: 50%;
  background: #fff7ed;
  border: 1px solid #fed7aa;
  color: var(--color-primary);
  font-size: 16px;
  line-height: 1;
  font-weight: 500;
}

.dropdown-empty {
  padding: var(--spacing-md);
  text-align: center;
  color: var(--color-text-muted);
  font-size: var(--font-size-sm);
}

.dropdown-list {
  display: flex;
  flex-direction: column;
}

.dropdown-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: var(--spacing-sm) var(--spacing-md);
  border-bottom: 1px solid var(--color-border);
  transition: background 0.2s;
}

.dropdown-item:last-child {
  border-bottom: none;
}

.dropdown-item:hover {
  background: var(--color-bg-base);
}

.dropdown-item.active {
  background: #fff7f0;
}

.item-content {
  flex: 1;
  cursor: pointer;
}

.item-name {
  font-size: var(--font-size-sm);
  font-weight: 400;
  color: var(--color-text-primary);
  margin-bottom: 0.25rem;
}

.item-meta {
  font-size: var(--font-size-xs);
  color: var(--color-text-muted);
}

.item-actions {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.btn-delete {
  width: 24px;
  height: 24px;
  padding: 0;
  background: transparent;
  border: none;
  color: var(--color-text-muted);
  font-size: 20px;
  cursor: pointer;
  transition: color 0.2s;
}

.btn-delete:hover {
  color: var(--color-negative);
}

.confirm-delete {
  display: flex;
  gap: 0.25rem;
}

.btn-confirm-delete,
.btn-cancel-delete {
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  font-size: var(--font-size-xs);
  cursor: pointer;
  transition: all 0.2s;
}

.btn-confirm-delete {
  background: var(--color-negative);
  border: 1px solid var(--color-negative);
  color: white;
}

.btn-confirm-delete:hover {
  background: var(--color-negative);
  filter: brightness(0.9);
}

.btn-cancel-delete {
  background: white;
  border: 1px solid var(--color-border);
  color: var(--color-text-muted);
}

.btn-cancel-delete:hover {
  border-color: var(--color-border-hover);
}
</style>
