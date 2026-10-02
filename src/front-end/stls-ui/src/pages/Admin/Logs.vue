<template>
  <div class="dashboard-container">
    <h2>System Logs Dashboard</h2>

    <!-- Filters -->
    <div class="filters">
      <input v-model="filters.Layer" placeholder="Layer" />
      <input v-model="filters.Service" placeholder="Service" />
      <input v-model="filters.Type" placeholder="Type" />
      <input v-model="filters.From" type="date" placeholder="From" />
      <input v-model="filters.To" type="date" placeholder="To" />

      <div class="actions">
        <button @click="fetchLogs" class="btn-primary">Search</button>
        <button @click="exportLogs" class="btn-export">Export CSV</button>
        <button @click="openPortainer" class="btn-dashboard">Dashboard</button>
        <button @click="openRabbitMQ" class="btn-dashboard">RabbitMQ</button>
      </div>
    </div>

    <!-- Error -->
    <div v-if="error" class="error">{{ error }}</div>

    <!-- Loading -->
    <div v-if="isLoading" class="loading">Loading logs...</div>

    <!-- Logs Table -->
    <div v-if="logs.length" class="table-container">
      <table>
        <thead>
          <tr>
            <th>Timestamp</th>
            <th>Layer</th>
            <th>Service</th>
            <th>Type</th>
            <th>Message</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="log in logs" :key="log.id">
            <td>{{ log.timestamp }}</td>
            <td>{{ log.layer }}</td>
            <td>{{ log.service }}</td>
            <td>{{ log.type }}</td>
            <td>{{ log.message }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-else-if="!isLoading" class="no-logs">No logs found.</div>
  </div>
</template>

<script setup>
import { onMounted, computed } from "vue";
import { useLogStore } from "../../stores/logStore";
import "../../assets/logs.css";

const logStore = useLogStore();

// Reactive references
const logs = computed(() => logStore.logs);
const isLoading = computed(() => logStore.isLoading);
const error = computed(() => logStore.error);
const filters = logStore.filters;

// ===============================
// Actions
// ===============================
function fetchLogs() {
  logStore.fetchLogs();
}

function clearFilters() {
  logStore.clearFilters();
}

function exportLogs() {
  logStore.exportLogsToFile("csv");
}

function openPortainer() {
  window.open("https://localhost:9443/#!/auth", "_blank", "noopener");
}

function openRabbitMQ() {
  window.open("http://localhost:15672/#/", "_blank", "noopener");
}


// Automatically fetch logs on mount
onMounted(() => {
  fetchLogs();
});
</script>

<style scoped>
.actions {
  display: flex;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.btn-dashboard {
  background-color: #3b82f6;
  color: white;
  border: none;
  padding: 0.4rem 0.8rem;
  border-radius: 6px;
  cursor: pointer;
  transition: background-color 0.2s ease;
}

.btn-dashboard:hover {
  background-color: #2563eb;
}
</style>
