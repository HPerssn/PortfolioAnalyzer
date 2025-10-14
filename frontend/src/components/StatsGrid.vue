<script setup lang="ts">
import StatCard from '@/components/StatCard.vue'
import { formatCurrency, formatPercentage } from '@/utils/formatters'
import type { BenchmarkComparison } from '@/types/portfolio'
import { computed } from 'vue'

interface Props {
  totalValue: number
  totalReturn: number
  totalReturnPercentage: number
  assetCount: number
  benchmarkComparison: BenchmarkComparison | null
}

const props = defineProps<Props>()

const benchmarkLabel = computed(() => {
  if (!props.benchmarkComparison) return 'vs Market'
  return `vs ${props.benchmarkComparison.benchmarkName}`
})

const benchmarkValue = computed(() => {
  if (!props.benchmarkComparison) return '—'
  const diff = props.benchmarkComparison.difference
  return diff >= 0 ? `${formatPercentage(diff)}` : formatPercentage(diff)
})

const benchmarkChange = computed(() => {
  if (!props.benchmarkComparison) return ''
  return `${Math.floor(props.benchmarkComparison.daysHeld / 365)}y ${props.benchmarkComparison.daysHeld % 365}d`
})

const benchmarkVariant = computed(() => {
  if (!props.benchmarkComparison) return undefined
  return props.benchmarkComparison.outperforming ? 'positive' : 'negative'
})
</script>

<template>
  <div class="stats-grid">
    <StatCard icon="dollar" label="Total Value" :value="formatCurrency(totalValue)" />

    <StatCard
      icon="bar-chart"
      :label="benchmarkLabel"
      :value="benchmarkValue"
      :change="benchmarkChange"
      :variant="benchmarkVariant"
    />

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
