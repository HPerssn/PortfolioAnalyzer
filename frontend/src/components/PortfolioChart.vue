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
import { formatChartDate, formatChartDateAdaptive } from '@/utils/formatters'
import { useMonteCarloStore } from '@/stores/monteCarloStore'
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

// Accept history data and timeframe as props
const props = defineProps<{
  history?: PortfolioHistoryPoint[]
  timeframe?: '1M' | '3M' | '1Y' | '5Y'
}>()

// Default to 1Y if timeframe not provided
const currentTimeframe = computed(() => props.timeframe ?? '1Y')

const monteCarloStore = useMonteCarloStore()

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

// Memoized historical data processing (computed once, doesn't change during simulation)
const historicalDataMemo = computed(() => {
  const historical = monteCarloStore.historicalData
  return {
    values: historical.map((point: PortfolioHistoryPoint) => point.value),
    labels: historical.map((point: PortfolioHistoryPoint) => formatChartDateAdaptive(point.date, currentTimeframe.value)),
    rawDates: historical.map((point: PortfolioHistoryPoint) => point.date),
    length: historical.length,
  }
})

// Process real data or use placeholder
const chartDataPoints = computed(() => {
  // Check if we have simulation data
  const hasSimulation = monteCarloStore.simulationState.isPlaying || monteCarloStore.simulationState.isPaused || monteCarloStore.simulationState.isComplete

  if (hasSimulation && monteCarloStore.simulationPercentiles.p50.length > 0) {
    // Use percentile bands for simulation
    const currentIndex = monteCarloStore.simulationState.currentIndex
    const historical = historicalDataMemo.value

    // Pre-calculate simulation slices (faster than multiple slice operations)
    const p25 = monteCarloStore.simulationPercentiles.p25
    const p50 = monteCarloStore.simulationPercentiles.p50
    const p75 = monteCarloStore.simulationPercentiles.p75

    // Build labels efficiently - reuse historical labels
    const simulationLabels: string[] = []
    for (let i = 1; i < currentIndex && i < p50.length; i++) {
      const point = p50[i]
      if (point) {
        simulationLabels.push(formatChartDateAdaptive(point.date, currentTimeframe.value))
      }
    }
    const labels = [...historical.labels, ...simulationLabels]

    // Build value arrays efficiently with pre-allocated nulls
    const nullPadding = new Array(historical.length - 1).fill(null)

    // Build simulation value arrays
    const p25Values = [...nullPadding]
    const p50Values = [...nullPadding]
    const p75Values = [...nullPadding]

    for (let i = 0; i < currentIndex && i < p50.length; i++) {
      p25Values.push(p25[i]?.value ?? 0)
      p50Values.push(p50[i]?.value ?? 0)
      p75Values.push(p75[i]?.value ?? 0)
    }

    return {
      labels,
      historicalValues: historical.values,
      p25Values,
      p50Values,
      p75Values,
      hasSimulation: true,
    }
  } else if (props.history && props.history.length > 0) {
    // Use real data with adaptive formatting
    const labels = props.history.map((point) => formatChartDateAdaptive(point.date, currentTimeframe.value))
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
  const showBand = monteCarloStore.showConfidenceBand

  if (dataPoints.hasSimulation) {
    const datasets: any[] = [
      // Historical data (solid orange line)
      {
        label: 'Historical',
        data: dataPoints.historicalValues,
        borderColor: '#f97316',
        backgroundColor: 'rgba(249, 115, 22, 0.02)',
        borderWidth: 1.5,
        tension: 0.4,
        pointRadius: 0,
        pointHoverRadius: 4,
        pointHoverBackgroundColor: '#f97316',
        pointHoverBorderColor: '#fff',
        pointHoverBorderWidth: 2,
        fill: true,
        order: 1,
      },
    ]

    // Add confidence band if enabled
    if (showBand) {
      // P75 (upper bound with subtle border)
      datasets.push({
        label: '75th Percentile',
        data: dataPoints.p75Values,
        borderColor: 'rgba(249, 115, 22, 0.45)',
        backgroundColor: 'transparent',
        borderWidth: 1,
        borderDash: [4, 3],
        tension: 0.4,
        pointRadius: 0,
        pointHoverRadius: 0,
        fill: false,
        order: 3,
      })

      // P25 (lower bound with fill to P75)
      datasets.push({
        label: '25th-75th Percentile Range',
        data: dataPoints.p25Values,
        borderColor: 'rgba(249, 115, 22, 0.45)',
        backgroundColor: 'rgba(249, 115, 22, 0.08)',
        borderWidth: 1,
        borderDash: [4, 3],
        tension: 0.4,
        pointRadius: 0,
        pointHoverRadius: 0,
        fill: '-1', // Fill to previous dataset (P75)
        order: 3,
      })
    }

    // P50 Median (dashed orange line - always shown)
    datasets.push({
      label: 'Median Projection',
      data: dataPoints.p50Values,
      borderColor: '#f97316',
      backgroundColor: 'transparent',
      borderWidth: 1.5,
      borderDash: [5, 5],
      tension: 0.4,
      pointRadius: 0,
      pointHoverRadius: 4,
      pointHoverBackgroundColor: '#f97316',
      pointHoverBorderColor: '#fff',
      pointHoverBorderWidth: 2,
      fill: false,
      order: 2,
    })

    return {
      labels: dataPoints.labels,
      datasets,
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
      padding: 10,
      displayColors: false,
      titleFont: {
        size: 11,
        weight: 'normal' as const,
        family: "'Inter', sans-serif",
      },
      bodyFont: {
        size: 12,
        weight: 'normal' as const,
        family: "'Inter', sans-serif",
      },
      callbacks: {
        label: (context: any) => {
          const value = context.parsed?.y
          if (value == null) return ''

          const datasetLabel = context.dataset.label
          const isSimulation = datasetLabel && (datasetLabel.includes('Projection') || datasetLabel.includes('Percentile'))

          if (isSimulation) {
            const dataIndex = context.dataIndex
            const historicalLength = monteCarloStore.historicalData.length
            const simulationIndex = dataIndex - historicalLength + 1

            if (simulationIndex >= 0 && simulationIndex < monteCarloStore.simulationPercentiles.p50.length) {
              const p25 = monteCarloStore.simulationPercentiles.p25[simulationIndex]?.value ?? 0
              const p50 = monteCarloStore.simulationPercentiles.p50[simulationIndex]?.value ?? 0
              const p75 = monteCarloStore.simulationPercentiles.p75[simulationIndex]?.value ?? 0

              // Calculate mean from percentiles (approximation)
              const mean = Math.round((p25 + p50 + p75) / 3)

              // Only show for the median projection line to avoid duplicates
              if (datasetLabel === 'Median Projection') {
                return [
                  `High: $${p75.toLocaleString('en-US')}`,
                  `Mean: $${mean.toLocaleString('en-US')}`,
                  `Low: $${p25.toLocaleString('en-US')}`
                ]
              }
              // For other simulation datasets, return empty to hide them
              return ''
            }
          }

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
        // Adaptive maxTicksLimit based on timeframe
        maxTicksLimit: currentTimeframe.value === '5Y' ? 6 : currentTimeframe.value === '1Y' ? 12 : 8,
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
