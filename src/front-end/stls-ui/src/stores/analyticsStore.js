import { defineStore } from "pinia";
import { ref } from "vue";
import { analyticsApi } from "../services/httpClients";

export const useAnalyticsStore = defineStore("analytics", () => {
  // ===============================
  // State
  // ===============================
  const latestAlerts = ref([]);
  const summaries = ref([]);
  const loading = ref(false);
  const error = ref(null);

  // ===============================
  // Fetch Alerts
  // ===============================
  async function fetchAlerts({ type, intersection, from, to } = {}) {
    loading.value = true;
    error.value = null;
    try {
      const params = {};
      if (type) params.type = type;
      if (intersection) params.intersection = intersection;
      if (from) params.from = from;
      if (to) params.to = to;

      const res = await analyticsApi.get("/api/analytics/alerts", { params });
      latestAlerts.value = res.data;
      console.log("[AnalyticsStore] fetched alerts:", latestAlerts.value);
    } catch (err) {
      error.value =
        err.response?.status
          ? `[HTTP ${err.response.status}]`
          : err.message || "Network error";
      latestAlerts.value = [];
      console.error("[AnalyticsStore] fetchAlerts error:", err);
    } finally {
      loading.value = false;
    }
  }

  // ===============================
  // Fetch Summaries
  // ===============================
  async function fetchSummaries({ intersectionId, intersection, from, to } = {}) {
    loading.value = true;
    error.value = null;
    try {
      const params = {};
      if (intersectionId !== undefined) params.intersectionId = intersectionId;
      if (intersection) params.intersection = intersection;
      if (from) params.from = from;
      if (to) params.to = to;

      const res = await analyticsApi.get("/api/analytics/summaries", { params });
      summaries.value = res.data;
      console.log("[AnalyticsStore] fetched summaries:", summaries.value);
    } catch (err) {
      error.value =
        err.response?.status
          ? `[HTTP ${err.response.status}]`
          : err.message || "Network error";
      summaries.value = [];
      console.error("[AnalyticsStore] fetchSummaries error:", err);
    } finally {
      loading.value = false;
    }
  }

  async function exportSummariesCsv({ intersectionId, intersection, from, to } = {}) {
  loading.value = true;
  error.value = null;
  try {
    const params = {};
    if (intersectionId !== undefined) params.intersectionId = intersectionId;
    if (intersection) params.intersection = intersection;
    if (from) params.from = from;
    if (to) params.to = to;

    const res = await analyticsApi.get("/api/analytics/summaries/export", {
      params,
      responseType: "blob"
    });

    // Trigger browser download
    const blob = new Blob([res.data], { type: "text/csv;charset=utf-8;" });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = `traffic_summaries_${new Date().toISOString().split("T")[0]}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(url);

    console.log("[AnalyticsStore] Exported summaries to CSV");
  } catch (err) {
    error.value =
      err.response?.status
        ? `[HTTP ${err.response.status}]`
        : err.message || "Network error";
    console.error("[AnalyticsStore] exportSummariesCsv error:", err);
  } finally {
    loading.value = false;
  }
}


  // ===============================
  // Return state & actions
  // ===============================
  return {
    latestAlerts,
    summaries,
    loading,
    error,
    fetchAlerts,
    fetchSummaries,
    exportSummariesCsv
  };
});
