<script setup lang="ts">
defineProps<{
  type: 'dollar' | 'arrow-up' | 'bar-chart' | 'pie-chart'
  size?: number
}>()

const icons = {
  'dollar': [
    [0, 1, 1, 1, 0],
    [1, 1, 0, 0, 0],
    [0, 1, 1, 1, 0],
    [0, 0, 0, 1, 1],
    [1, 1, 1, 1, 0]
  ],
  'arrow-up': [
    [0, 0, 1, 0, 0],
    [0, 1, 1, 1, 0],
    [1, 0, 1, 0, 1],
    [0, 0, 1, 0, 0],
    [0, 0, 1, 0, 0]
  ],
  'bar-chart': [
    [0, 0, 0, 0, 1],
    [0, 0, 1, 0, 1],
    [0, 1, 1, 0, 1],
    [1, 1, 1, 0, 1],
    [1, 1, 1, 1, 1]
  ],
  'pie-chart': [
    [0, 1, 1, 1, 0],
    [1, 1, 1, 0, 1],
    [1, 1, 0, 0, 1],
    [1, 0, 0, 0, 1],
    [0, 1, 1, 1, 0]
  ]
}
</script>

<template>
  <div class="pixel-icon" :style="{ width: `${size || 20}px`, height: `${size || 20}px` }">
    <div v-for="(row, rowIndex) in icons[type]" :key="rowIndex" class="pixel-row">
      <div
        v-for="(pixel, colIndex) in row"
        :key="colIndex"
        class="pixel"
        :class="{ active: pixel === 1 }"
      ></div>
    </div>
  </div>
</template>

<style scoped>
.pixel-icon {
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.pixel-row {
  display: flex;
  gap: 1px;
  flex: 1;
}

.pixel {
  flex: 1;
  background: transparent;
  transition: background 0.2s;
}

.pixel.active {
  background: currentColor;
}
</style>
