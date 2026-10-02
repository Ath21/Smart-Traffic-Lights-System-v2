<template>
  <div class="chart-container">
    <h3>{{ title }}</h3>
    <!-- add :key to Line so it re-renders when chartData changes -->
    <Line
      v-if="chartData && chartData.labels?.length"
      ref="chartRef"
      :data="chartData"
      :options="chartOptions"
      :key="chartDataKey"
    />
    <div v-else class="no-data">No data available</div>
  </div>
</template>

<script setup>
import { defineProps, ref, watch, computed } from "vue"
import {
  Chart as ChartJS,
  Title,
  Tooltip,
  Legend,
  LineElement,
  CategoryScale,
  LinearScale,
  PointElement
} from "chart.js"
import { Line } from "vue-chartjs"

ChartJS.register(Title, Tooltip, Legend, LineElement, CategoryScale, LinearScale, PointElement)

const props = defineProps({
  chartData: { type: Object, required: true },
  title: { type: String, default: "" }
})

const chartRef = ref(null)

// computed key — changes whenever labels or datasets change
const chartDataKey = computed(() =>
  JSON.stringify([
    props.chartData.labels,
    props.chartData.datasets?.map(d => d.data)
  ])
)

// optional: ensure Chart.js updates if only dataset values change
watch(
  () => props.chartData,
  () => {
    if (chartRef.value?.chart) {
      chartRef.value.chart.update()
    }
  },
  { deep: true }
)

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { position: "top" },
    title: { display: false }
  },
  scales: {
    x: { ticks: { autoSkip: false } },
    y: { beginAtZero: true }
  }
}
</script>

<style scoped>
.chart-container {
  width: 100%;
  height: 100%;
  min-height: 300px;
  display: flex;
  flex-direction: column;
}
.chart-container h3 {
  text-align: center;
  margin-bottom: 0.5rem;
}
.no-data {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #888;
  font-style: italic;
}
</style>
