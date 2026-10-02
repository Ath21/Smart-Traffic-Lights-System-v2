<template>
  <div class="audits-page">
    <h2 class="page-title">User Audits</h2>

    <div class="controls">
      <input
        v-model="userIdInput"
        type="number"
        placeholder="Enter User ID"
        class="input-userid"
      />
      <button class="btn" @click="fetchAudits">Fetch Audits</button>
    </div>

    <div v-if="loading" class="loading">Loading audits...</div>

    <table v-else-if="audits.length" class="audits-table">
      <thead>
        <tr>
          <th>Audit ID</th>
          <th>User ID</th>
          <th>Action</th>
          <th>Details</th>
          <th>Timestamp</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="a in audits" :key="a.AuditId">
          <td>{{ a.AuditId }}</td>
          <td>{{ a.UserId }}</td>
          <td>{{ a.Action }}</td>
          <td>{{ a.Details }}</td>
          <td>{{ new Date(a.Timestamp).toLocaleString() }}</td>
        </tr>
      </tbody>
    </table>

    <div
      v-else-if="!loading && audits.length === 0 && userIdInput"
      class="empty"
    >
      No audits found for User ID {{ userIdInput }}.
    </div>

    <div v-if="error" class="error">{{ error }}</div>
  </div>
</template>

<script setup>
import { ref } from "vue";
import { useUserStore } from "../../stores/userStore";
import { userApi } from "../../services/httpClients";
import "../../assets/audits.css";

const userStore = useUserStore();

const userIdInput = ref("");
const audits = ref([]);
const loading = ref(false);
const error = ref(null);

// ------------------------------
// Fetch audits
// ------------------------------
async function fetchAudits() {
  if (!userIdInput.value) return;

  loading.value = true;
  error.value = null;
  audits.value = [];

  try {
    const res = await userApi.get(
      `${import.meta.env.VITE_USER_API || "http://localhost:5055"}/api/audit/user/${userIdInput.value}`,
      {
        headers: {
          Authorization: `Bearer ${userStore.token}`,
        },
      }
    );

    audits.value = res.data || [];
  } catch (err) {
    console.error("[AuditsPage] fetchAudits error:", err);
    error.value = err.response?.data?.message || err.message || "Failed to fetch audits";
  } finally {
    loading.value = false;
  }
}
</script>
