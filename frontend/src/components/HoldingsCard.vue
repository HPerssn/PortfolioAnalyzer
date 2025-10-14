<script setup lang="ts">
import { computed, ref } from 'vue'
import type { Asset } from '@/types/portfolio'
import PixelIcon from '@/components/PixelIcon.vue'

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

// Show legend on hover
const showLegend = ref(false)

// Assets with their allocation percentages
const assetsWithAllocation = computed(() =>
  props.assets.map((asset, index) => ({
    symbol: asset.symbol,
    percentage: ((asset.currentValue / props.totalValue) * 100).toFixed(1),
    color: getAssetColor(index)
  }))
)
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
        <div
          class="allocation-bar-container"
          @mouseenter="showLegend = true"
          @mouseleave="showLegend = false"
        >
          <div
            v-for="(asset, index) in props.assets"
            :key="asset.symbol"
            class="allocation-bar"
            :style="{
              width: `${(asset.currentValue / props.totalValue) * 100}%`,
              background: getAssetColor(index)
            }"
          ></div>
        </div>

        <!-- Allocation Legend -->
        <Transition name="legend-fade">
          <div v-if="showLegend" class="allocation-legend">
            <div
              v-for="item in assetsWithAllocation"
              :key="item.symbol"
              class="legend-item"
            >
              <div class="legend-color" :style="{ background: item.color }"></div>
              <span class="legend-symbol">{{ item.symbol }}</span>
              <span class="legend-percent">{{ item.percentage }}%</span>
            </div>
          </div>
        </Transition>
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

/* --- LEGEND STYLES --- */
.allocation-legend {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  margin-top: 8px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px 12px;
  padding: 12px;
  background: var(--color-card-bg);
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
  z-index: 100;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: var(--font-size-xs);
}

.legend-color {
  width: 10px;
  height: 10px;
  border-radius: 2px;
  flex-shrink: 0;
}

.legend-symbol {
  color: var(--color-text-primary);
  font-weight: 500;
  letter-spacing: 0.02em;
}

.legend-percent {
  color: var(--color-text-muted);
  font-weight: 300;
}

/* Legend fade transition */
.legend-fade-enter-active,
.legend-fade-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.legend-fade-enter-from,
.legend-fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>