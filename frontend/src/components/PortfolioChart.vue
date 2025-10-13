<script setup lang="ts">
import { computed } from 'vue'
import { Line } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
  type ChartOptions,
  type ChartData,
} from 'chart.js'
import type { PortfolioHistoryPoint } from '@/types/portfolio'

// Register Chart.js components
ChartJS.register(
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Legend,
  Filler,
)

// Accept history data as a prop
const props = defineProps<{
  history?: PortfolioHistoryPoint[]
}>()

// Generate placeholder data if no real data is provided
const generatePlaceholderData = () => {
  const data = []
  const labels = []
  const today = new Date()
  const startValue = 50000
  let currentValue = startValue

  for (let i = 365; i >= 0; i -= 7) {
    const date = new Date(today)
    date.setDate(date.getDate() - i)

    const month = date.toLocaleDateString('en-US', { month: 'short' })
    const day = date.getDate()
    labels.push(`${month} ${day}`)

    const randomChange = (Math.random() - 0.45) * 2000
    currentValue = Math.max(startValue * 0.8, currentValue + randomChange)
    data.push(Math.round(currentValue))
  }

  return { labels, data }
}

// Process real data or use placeholder
const chartDataPoints = computed(() => {
  if (props.history && props.history.length > 0) {
    // Use real data
    const labels = props.history.map((point) => {
      const date = new Date(point.date)
      const month = date.toLocaleDateString('en-US', { month: 'short' })
      const day = date.getDate()
      return `${month} ${day}`
    })
    const data = props.history.map((point) => point.value)
    return { labels, data }
  } else {
    // Use placeholder
    return generatePlaceholderData()
  }
})

// Chart data configuration
const chartData = computed<ChartData<'line'>>(() => ({
  labels: chartDataPoints.value.labels,
  datasets: [
    {
      label: 'Portfolio Value',
      data: chartDataPoints.value.data,
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
}))

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
