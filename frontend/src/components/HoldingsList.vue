<script setup lang="ts">
import type { Asset } from '@/types/portfolio'
import { formatCurrency, formatPercentage } from '@/utils/formatters'

interface Props {
  assets: Asset[]
}

defineProps<Props>()
</script>

<template>
  <div class="sidebar-section">
    <h3>Holdings</h3>
    <div class="holdings-list">
      <div v-for="asset in assets" :key="asset.symbol" class="holding-item">
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
</template>

<style scoped>
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

.holdings-list {
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
</style>
