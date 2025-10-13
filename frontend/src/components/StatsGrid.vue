<script setup lang="ts">
import StatCard from '@/components/StatCard.vue'
import { formatCurrency, formatPercentage } from '@/utils/formatters'

interface Props {
  totalValue: number
  totalReturn: number
  totalReturnPercentage: number
  assetCount: number
}

defineProps<Props>()
</script>

<template>
  <div class="stats-grid">
    <StatCard icon="dollar" label="Total Value" :value="formatCurrency(totalValue)" />

    <StatCard icon="bar-chart" label="Day Change" value="$0.00" change="+0.00%" />

    <StatCard
      icon="arrow-up"
      label="Total Return"
      :value="formatCurrency(totalReturn)"
      :change="formatPercentage(totalReturnPercentage)"
      :variant="totalReturn >= 0 ? 'positive' : 'negative'"
    />

    <StatCard icon="pie-chart" label="Holdings" :value="String(assetCount)" />
  </div>
</template>

<style scoped>
.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: var(--spacing-md);
  padding: var(--spacing-lg) var(--spacing-xl);
  background: var(--color-bg-base);
}

/* Tablets (<= 1024px) */
@media (max-width: 1024px) {
  .stats-grid {
    grid-template-columns: repeat(2, 1fr);
    gap: var(--spacing-md);
  }
}

/* Mobile (<= 768px) */
@media (max-width: 768px) {
  .stats-grid {
    grid-template-columns: 1fr;
  }
}
</style>
