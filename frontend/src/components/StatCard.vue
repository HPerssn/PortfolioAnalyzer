<script setup lang="ts">
import PixelIcon from '@/components/PixelIcon.vue'

interface Props {
  icon: 'dollar' | 'bar-chart' | 'arrow-up' | 'pie-chart'
  label: string
  value: string
  change?: string
  variant?: 'default' | 'positive' | 'negative'
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'default',
})
</script>

<template>
  <div class="stat-card" :class="variant">
    <div class="stat-icon">
      <PixelIcon :type="icon" :size="40" />
    </div>
    <div class="stat-info">
      <div class="stat-label">{{ label }}</div>
      <div class="stat-value">{{ value }}</div>
      <div v-if="change" class="stat-change">{{ change }}</div>
    </div>
  </div>
</template>

<style scoped>
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
  background: rgba(16, 163, 74, 0.1);
  color: var(--color-positive);
}

/* Mobile optimizations */
@media (max-width: 768px) {
  .stat-card {
    flex-direction: row;
    align-items: center;
  }

  .stat-value {
    font-size: 1.2rem;
  }

  .stat-icon {
    display: none;
  }
}

@media (max-width: 480px) {
  .stat-card {
    padding: var(--spacing-md);
  }
}
</style>
