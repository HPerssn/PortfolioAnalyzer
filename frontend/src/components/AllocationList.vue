<script setup lang="ts">
import type { Asset } from '@/types/portfolio'
import { ASSET_COLORS, DEFAULT_COLOR } from '@/constants/colors'

interface Props {
  assets: Asset[]
  totalValue: number
}

defineProps<Props>()

const getAssetColor = (symbol: string): string => {
  const hash = symbol.split('').reduce((acc, char) => acc + char.charCodeAt(0), 0)
  return ASSET_COLORS[hash % ASSET_COLORS.length] || DEFAULT_COLOR
}
</script>

<template>
  <div class="sidebar-section">
    <h3>Allocation</h3>
    <div class="allocation-list">
      <div v-for="asset in assets" :key="asset.symbol" class="allocation-item">
        <div class="allocation-color" :style="{ background: getAssetColor(asset.symbol) }"></div>
        <span class="allocation-label">{{ asset.symbol }}</span>
        <span class="allocation-percent"
          >{{ ((asset.currentValue / totalValue) * 100).toFixed(1) }}%</span
        >
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

.allocation-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
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
</style>
