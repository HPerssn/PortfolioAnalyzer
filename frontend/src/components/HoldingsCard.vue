<script setup lang="ts">
import { computed } from 'vue'
import type { Asset } from '@/types/portfolio'
import PixelIcon from '@/components/PixelIcon.vue'
import { formatPercentage } from '@/utils/formatters'

interface Props {
  assets: Asset[]
  totalValue: number
}

const props = defineProps<Props>()

// FIX: Ensure five distinct shades are used for up to five assets before repeating
const ORANGE_SHADES = ['#f97316', '#fb923c', '#fdba74', '#fed7aa', '#ffe5d0']
const getAssetColor = (index: number) => ORANGE_SHADES[index % ORANGE_SHADES.length]

// Holdings Count
const holdingsCount = computed(() => props.assets.length)

// Check if assets exist
const hasAssets = computed(() => props.assets.length > 0 && props.totalValue > 0)
</script>

<template>
  <div class="stat-card holdings-card">
    <div class="stat-icon">
      <PixelIcon type="pie-chart" :size="40" />
    </div>

    <div class="stat-info">
      <div class="stat-label">ALLOCATION</div>
      
      <div class="stat-value">{{ holdingsCount }}</div>

      <div v-if="hasAssets" class="allocation-bar-wrapper expanded">
        <div class="allocation-bar-container">
          <div
            v-for="(asset, index) in props.assets"
            :key="asset.symbol"
            class="allocation-bar"
            :style="{ 
              width: `${(asset.currentValue / props.totalValue) * 100}%`, 
              background: getAssetColor(index) 
            }"
            
            :title="`${asset.symbol}: ${formatPercentage(asset.currentValue / props.totalValue)}`"
          ></div>
        </div>
      </div>

      <div v-else class="no-holdings">No assets in portfolio</div>
    </div>
  </div>
</template>

---

<style scoped>
/* --- LAYOUT FIXES (Top Alignment) --- */
.stat-card {
  display: flex;
  gap: var(--spacing-lg);
  background: var(--color-card-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  padding: var(--spacing-lg);
  flex-direction: row;
  align-items: flex-start;
  /* Add relative positioning and z-index to ensure tooltip displays above other elements */
  position: relative;
  z-index: 2; 
}

.stat-icon {
  color: var(--color-text-muted);
  flex-shrink: 0;
  display: flex;
  align-items: flex-start;
}

.stat-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  justify-content: flex-start;
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
  margin-bottom: 8px;
}

/* --- ALLOCATION BAR STYLES (12px Height) --- */
.allocation-bar-wrapper.expanded {
  position: relative;
  flex: 1;
  min-height: 0;
}

.allocation-bar-container {
  position: relative;
  display: flex;
  height: 12px; 
  border-radius: 4px;
  overflow: hidden;
  background: var(--color-border);
  /* Ensure the container is positioned relative for z-index to work */
  z-index: 3; 
}

.allocation-bar {
  height: 100%;
  transition: width 0.3s ease;
  /* Crucial for mouse events and tooltip visibility */
  pointer-events: all;
  z-index: 4; 
}

.no-holdings {
  font-size: var(--font-size-sm);
  color: var(--color-text-muted);
  margin-top: 4px;
}
</style>