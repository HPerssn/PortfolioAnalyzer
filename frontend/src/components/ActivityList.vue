<script setup lang="ts">
interface ActivityItem {
  type: 'buy' | 'sell'
  symbol: string
  quantity: number
  date: string
}

interface Props {
  activities: ActivityItem[]
}

defineProps<Props>()
</script>

<template>
  <div class="sidebar-section">
    <h3>Activity</h3>
    <div class="activity-list">
      <div v-for="(activity, index) in activities" :key="index" class="activity-item">
        <span class="activity-type" :class="activity.type">{{ activity.type.toUpperCase() }}</span>
        <span class="activity-text">{{ activity.symbol }} · {{ activity.quantity }} shares</span>
        <span class="activity-date">{{ activity.date }}</span>
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

.activity-list {
  display: flex;
  flex-direction: column;
  gap: var(--spacing-md);
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

.activity-type.buy {
  background: rgba(16, 163, 74, 0.1);
  color: var(--color-positive);
}

.activity-type.sell {
  background: rgba(220, 38, 38, 0.1);
  color: var(--color-negative);
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
