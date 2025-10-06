<script setup lang="ts">
import type { Asset } from '@/types/portfolio'

defineProps<{
  assets: Asset[]
}>()

const formatCurrency = (value: number) => {
  return new Intl.NumberFormat('sv-SE', {
    style: 'currency',
    currency: 'USD'
  }).format(value)
}

const formatPercentage = (value: number) => {
  return `${value >= 0 ? '+' : ''}${value.toFixed(2)}%`
}

const getReturnClass = (value: number) => {
  return value >= 0 ? 'positive' : 'negative'
}
</script>

<template>
  <div class="asset-list">
    <h2>Holdings</h2>
    <div class="table-container">
      <table>
        <thead>
          <tr>
            <th>Symbol</th>
            <th class="text-right">Quantity</th>
            <th class="text-right">Avg Cost</th>
            <th class="text-right">Current Price</th>
            <th class="text-right">Total Cost</th>
            <th class="text-right">Current Value</th>
            <th class="text-right">Return</th>
            <th class="text-right">Return %</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="asset in assets" :key="asset.symbol">
            <td class="symbol">{{ asset.symbol }}</td>
            <td class="text-right">{{ asset.quantity }}</td>
            <td class="text-right">{{ formatCurrency(asset.averageCost) }}</td>
            <td class="text-right">{{ formatCurrency(asset.currentPrice) }}</td>
            <td class="text-right">{{ formatCurrency(asset.totalCost) }}</td>
            <td class="text-right">{{ formatCurrency(asset.currentValue) }}</td>
            <td class="text-right" :class="getReturnClass(asset.return)">
              {{ formatCurrency(asset.return) }}
            </td>
            <td class="text-right" :class="getReturnClass(asset.returnPercentage)">
              {{ formatPercentage(asset.returnPercentage) }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.asset-list {
  margin-top: var(--spacing-2xl);
}

.asset-list h2 {
  font-size: var(--font-size-lg);
  font-weight: 300;
  color: var(--color-text-primary);
  margin-bottom: var(--spacing-lg);
  letter-spacing: -0.01em;
}

.table-container {
  overflow-x: auto;
  border-radius: var(--radius-lg);
  background: var(--color-card-bg);
  border: 1px solid var(--color-border);
}

table {
  width: 100%;
  border-collapse: collapse;
}

thead {
  background: transparent;
  border-bottom: 1px solid var(--color-border);
}

th {
  padding: var(--spacing-md) var(--spacing-lg);
  font-size: var(--font-size-xs);
  font-weight: 400;
  color: var(--color-text-muted);
  text-transform: uppercase;
  letter-spacing: 0.05em;
  text-align: left;
}

tbody tr {
  border-bottom: 1px solid var(--color-border);
  transition: background 0.2s ease;
}

tbody tr:last-child {
  border-bottom: none;
}

tbody tr:hover {
  background: #fafafa;
}

td {
  padding: var(--spacing-md) var(--spacing-lg);
  font-size: var(--font-size-sm);
  color: var(--color-text-primary);
  font-weight: 300;
}

.symbol {
  font-weight: 400;
  color: var(--color-text-primary);
  font-size: var(--font-size-sm);
  letter-spacing: 0.02em;
}

.text-right {
  text-align: right;
}

.positive {
  color: var(--color-positive);
  font-weight: 400;
}

.negative {
  color: var(--color-negative);
  font-weight: 400;
}

@media (max-width: 768px) {
  .table-container {
    font-size: var(--font-size-xs);
  }

  th,
  td {
    padding: var(--spacing-sm);
  }
}
</style>
