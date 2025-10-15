<script setup lang="ts">
import { computed } from 'vue'
import { Line } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Tooltip,
  Filler,
  type ChartOptions,
  type ChartData,
} from 'chart.js'
import type { PortfolioHistoryPoint } from '@/types/portfolio'
import { formatChartDate } from '@/utils/formatters'
import { usePortfolioStore } from '@/stores/portfolioStore'
import {
  PLACEHOLDER_START_VALUE,
  PLACEHOLDER_DAYS_BACK,
  PLACEHOLDER_INTERVAL_DAYS,
  PLACEHOLDER_VOLATILITY,
  PLACEHOLDER_BIAS,
  PLACEHOLDER_MIN_MULTIPLIER,
} from '@/constants/chart'

// Register Chart.js components
ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Tooltip, Filler)

// Accept history data as a prop
const props = defineProps<{
  history?: PortfolioHistoryPoint[]
}>()

const portfolioStore = usePortfolioStore()

// Generate placeholder data if no real data is provided
const generatePlaceholderData = () => {
  const data = []
  const labels = []
  const today = new Date()
  let currentValue = PLACEHOLDER_START_VALUE

  for (let i = PLACEHOLDER_DAYS_BACK; i >= 0; i -= PLACEHOLDER_INTERVAL_DAYS) {
    const date = new Date(today)
    date.setDate(date.getDate() - i)
    labels.push(formatChartDate(date.toISOString()))

    const randomChange = (Math.random() - PLACEHOLDER_BIAS) * PLACEHOLDER_VOLATILITY
    currentValue = Math.max(
      PLACEHOLDER_START_VALUE * PLACEHOLDER_MIN_MULTIPLIER,
      currentValue + randomChange,
    )
    data.push(Math.round(currentValue))
  }

  return { labels, data }
}

// Process real data or use placeholder
const chartDataPoints = computed(() => {
  // Check if we have simulation data
  const hasSimulation = portfolioStore.simulationState.isPlaying || portfolioStore.simulationState.isPaused

  if (hasSimulation && portfolioStore.combinedChartData.length > 0) {
    // Use combined data (historical + simulation)
    const historicalLength = portfolioStore.historicalData.length
    const combinedData = portfolioStore.combinedChartData

    const labels = combinedData.map((point) => formatChartDate(point.date))
    const historicalValues = combinedData.slice(0, historicalLength).map((point) => point.value)
    const simulationValues = new Array(historicalLength).fill(null).concat(
      combinedData.slice(historicalLength).map((point) => point.value)
    )

    return { labels, historicalValues, simulationValues, hasSimulation: true }
  } else if (props.history && props.history.length > 0) {
    // Use real data
    const labels = props.history.map((point) => formatChartDate(point.date))
    const data = props.history.map((point) => point.value)
    return { labels, data, hasSimulation: false }
  } else {
    // Use placeholder
    const placeholder = generatePlaceholderData()
    return { ...placeholder, hasSimulation: false }
  }
})

// Chart data configuration
const chartData = computed<ChartData<'line'>>(() => {
  const dataPoints = chartDataPoints.value

  if (dataPoints.hasSimulation) {
    // Show two datasets: historical (solid) and simulation (dashed)
    return {
      labels: dataPoints.labels,
      datasets: [
        {
          label: 'Historical',
          data: dataPoints.historicalValues,
          borderColor: '#f97316', // Orange line
          backgroundColor: 'rgba(249, 115, 22, 0.02)', // Very subtle orange fill
          borderWidth: 1.5,
          tension: 0.4,
          pointRadius: 0,
          pointHoverRadius: 4,
          pointHoverBackgroundColor: '#f97316',
          pointHoverBorderColor: '#fff',
          pointHoverBorderWidth: 2,
          fill: true,
        },
        {
          label: 'Simulation',
          data: dataPoints.simulationValues,
          borderColor: '#f97316', // Same orange
          backgroundColor: 'transparent',
          borderWidth: 1.5,
          borderDash: [5, 5], // Dashed line
          tension: 0.4,
          pointRadius: 0,
          pointHoverRadius: 4,
          pointHoverBackgroundColor: '#f97316',
          pointHoverBorderColor: '#fff',
          pointHoverBorderWidth: 2,
          fill: false,
        },
      ],
    }
  } else {
    // Single dataset for regular view
    return {
      labels: dataPoints.labels,
      datasets: [
        {
          label: 'Portfolio Value',
          data: dataPoints.data,
          borderColor: '#f97316', // Orange line
          backgroundColor: 'rgba(249, 115, 22, 0.02)', // Very subtle orange fill
          borderWidth: 1.5, // Thin line
          tension: 0.4, // Smooth curved line
          pointRadius: 0, // Hide points by default
          pointHoverRadius: 4, // Show small point on hover
          pointHoverBackgroundColor: '#f97316',
          pointHoverBorderColor: '#fff',
          pointHoverBorderWidth: 2,
          fill: true, // Fill area under line
        },
      ],
    }
  }
})

// Chart options configuration
const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: {
    intersect: false,
    mode: 'index' as const,
  },
  plugins: {
    legend: {
      display: false, // Remove legend
    },
    tooltip: {
      enabled: true,
      backgroundColor: 'rgba(255, 255, 255, 0.95)',
      titleColor: '#404040',
      bodyColor: '#737373',
      borderColor: '#e5e5e5',
      borderWidth: 1,
      padding: 8,
      displayColors: false,
      titleFont: {
        size: 11,
        weight: 'normal' as const,
        family: "'Inter', sans-serif",
      },
      bodyFont: {
        size: 13,
        weight: 'normal' as const,
        family: "'Inter', sans-serif",
      },
      callbacks: {
        label: (context: any) => {
          const value = context.parsed?.y
          if (value == null) return ''
          return `$${value.toLocaleString('en-US')}`
        },
      },
    },
  },
  scales: {
    x: {
      display: true,
      grid: {
        display: true,
        color: 'rgba(0, 0, 0, 0.03)', // Very subtle grid lines
        lineWidth: 1,
      },
      ticks: {
        color: '#a3a3a3', // Light gray
        font: {
          size: 10,
          weight: 'normal' as const,
          family: "'Inter', sans-serif",
        },
        maxRotation: 0,
        autoSkipPadding: 20,
      },
      border: {
        display: false,
      },
    },
    y: {
      display: true,
      position: 'right' as const,
      grid: {
        display: true,
        color: 'rgba(0, 0, 0, 0.03)', // Very subtle grid lines
        lineWidth: 1,
      },
      ticks: {
        color: '#a3a3a3', // Light gray
        font: {
          size: 10,
          weight: 'normal' as const,
          family: "'Inter', sans-serif",
        },
        callback: (value: any) => {
          return `$${(value as number).toLocaleString('en-US', { maximumFractionDigits: 0 })}`
        },
        padding: 8,
      },
      border: {
        display: false,
      },
    },
  },
}) as ChartOptions<'line'>)
</script>

<template>
  <div class="chart-container">
    <Line :data="chartData" :options="chartOptions" />
  </div>
</template>

<style scoped>
.chart-container {
  flex: 1;
  position: relative;
  padding: 1rem;
  min-height: 0;
}
</style>
