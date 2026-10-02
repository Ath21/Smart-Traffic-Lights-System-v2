<template>
  <div class="analytics-page">
    <h2>Traffic Summaries</h2>

    <!-- Filters -->
    <div class="filters">
      <input type="text" v-model="intersection" placeholder="Intersection" />
      <input type="number" v-model.number="intersectionId" placeholder="Intersection ID" />
      <input type="date" v-model="from" />
      <input type="date" v-model="to" />

      <!-- Export Button -->
      <button class="export-btn" @click="handleExport" :disabled="loading">
        {{ loading ? 'Exporting...' : 'Export CSV' }}
      </button>
    </div>

    <!-- Loading & errors -->
    <div v-if="loading && !summaries.length">Loading summaries...</div>
    <div v-if="error" class="error">{{ error }}</div>

    <!-- Charts container -->
    <div v-if="!loading && summaries.length" class="charts-container">
      <div class="chart-wrapper">
        <LineChart 
          :chart-data="vehiclesChartData" 
          title="Vehicles per Day" 
          :key="vehiclesChartData.labels.join('-')" 
        />
      </div>
      <div class="chart-wrapper">
        <LineChart 
          :chart-data="pedestriansChartData" 
          title="Pedestrians per Day" 
          :key="pedestriansChartData.labels.join('-')" 
        />
      </div>
      <div class="chart-wrapper">
        <LineChart 
          :chart-data="cyclistsChartData" 
          title="Cyclists per Day" 
          :key="cyclistsChartData.labels.join('-')" 
        />
      </div>
      <div class="chart-wrapper">
        <LineChart 
          :chart-data="congestionChartData" 
          title="Average Wait Time (sec)" 
          :key="congestionChartData.labels.join('-')" 
        />
      </div>
    </div>

    <!-- Empty state -->
    <div v-if="!loading && !summaries.length" class="text-gray-500">
      No summaries found for the selected filters.
    </div>
  </div>
</template>

<script setup>
import { ref, watchEffect, onMounted } from "vue"
import { useAnalyticsStore } from "../../stores/analyticsStore"
import { storeToRefs } from "pinia"
import LineChart from "../../components/LineChart.vue"

const analytics = useAnalyticsStore()
const { summaries, loading, error } = storeToRefs(analytics)
const { fetchSummaries, exportSummariesCsv } = analytics

// Filters
const intersection = ref("")
const intersectionId = ref(4)
const from = ref(new Date(Date.now() - 7 * 24 * 60 * 60 * 1000).toISOString().split("T")[0])
const to = ref(new Date().toISOString().split("T")[0])

// Chart data refs
const vehiclesChartData = ref({ labels: [], datasets: [] })
const pedestriansChartData = ref({ labels: [], datasets: [] })
const cyclistsChartData = ref({ labels: [], datasets: [] })
const congestionChartData = ref({ labels: [], datasets: [] })

// Fetch summaries when filters change
watchEffect(() => {
  if (!intersectionId.value) return
  fetchSummaries({
    intersectionId: intersectionId.value,
    intersection: intersection.value,
    from: from.value,
    to: to.value
  })
})

// Export handler
function handleExport() {
  exportSummariesCsv({
    intersectionId: intersectionId.value,
    intersection: intersection.value,
    from: from.value,
    to: to.value
  })
}

// Update charts when summaries change
watchEffect(() => {
  const data = summaries.value || []
  const labels = data.map(s => new Date(s.Date).toLocaleDateString())

  vehiclesChartData.value = {
    labels,
    datasets: [{
      label: "Total Vehicles",
      data: data.map(s => s.TotalVehicles || 0),
      borderColor: "#1d4ed8",
      backgroundColor: "#1d4ed8AA",
      tension: 0.3,
      fill: true
    }]
  }

  pedestriansChartData.value = {
    labels,
    datasets: [{
      label: "Total Pedestrians",
      data: data.map(s => s.TotalPedestrians || 0),
      borderColor: "#059669",
      backgroundColor: "#059669AA",
      tension: 0.3,
      fill: true
    }]
  }

  cyclistsChartData.value = {
    labels,
    datasets: [{
      label: "Total Cyclists",
      data: data.map(s => s.TotalCyclists || 0),
      borderColor: "#b45309",
      backgroundColor: "#b45309AA",
      tension: 0.3,
      fill: true
    }]
  }

  congestionChartData.value = {
    labels,
    datasets: [{
      label: "Average Wait Time (sec)",
      data: data.map(s => s.AverageWaitTimeSec || 0),
      borderColor: "#dc2626",
      backgroundColor: "#dc2626AA",
      tension: 0.3,
      fill: true
    }]
  }
})

// Initial load
onMounted(() => {
  fetchSummaries({
    intersectionId: intersectionId.value,
    intersection: intersection.value,
    from: from.value,
    to: to.value
  })
})
</script>

<style scoped>
.analytics-page {
  padding: 3rem;
}
.filters {
  margin-bottom: 1rem;
}
.filters input {
  margin-right: 0.5rem;
  width: 150px;
}
.export-btn {
  padding: 0.4rem 1rem;
  background-color: #2563eb;
  color: white;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  transition: background 0.2s;
}
.export-btn:hover:not(:disabled) {
  background-color: #1d4ed8;
}
.export-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.error {
  color: red;
  margin-top: 1rem;
}
.charts-container {
  display: flex;
  flex-wrap: wrap;
  gap: 2rem;
  margin-top: 2rem;
}
.chart-wrapper {
  flex: 1 1 45%;
  min-width: 300px;
  max-width: 600px;
  height: 350px;
  display: flex;
  flex-direction: column;
}
</style>
