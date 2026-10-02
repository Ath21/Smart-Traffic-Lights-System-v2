import { defineStore } from "pinia";
import { ref } from "vue"; // ✅ ADD THIS
import { logApi } from "../services/httpClients";
import { useUserStore } from "./userStore";

export const useLogStore = defineStore("logStore", () => {
  // ===============================
  // State
  // ===============================
  const logs = ref([]);
  const isLoading = ref(false);
  const error = ref(null);
  const filters = ref({
    Layer: "",
    Service: "",
    Type: "",
    From: "",
    To: "",
  });

  // ===============================
  // Fetch logs with optional filters
  // ===============================
  async function fetchLogs(filtersOverride = null) {
    const userStore = useUserStore();
    if (!userStore.user || userStore.user.role.toLowerCase() !== "admin") return;

    isLoading.value = true;
    error.value = null;

    try {
      const params = filtersOverride || filters.value;
      const { data } = await logApi.get("/api/logs/search", { params });

      // Ensure data is an array
      const logsArray = Array.isArray(data) ? data : data.logs || [];

      logs.value = logsArray.map((log) => ({
        id: log.id,
        timestamp: log.Timestamp || log.timestamp || "",
        layer: log.Layer || log.layer || "",
        service: log.Service || log.service || "",
        type: log.Type || log.type || "",
        message: log.Message || log.message || "",
      }));
    } catch (err) {
      error.value = err.response?.data?.message || err.message || "Failed to fetch logs";
      console.error("[LogStore] fetchLogs error:", err);
      logs.value = [];
    } finally {
      isLoading.value = false;
    }
  }

  // ===============================
  // Export logs
  // ===============================
  async function exportLogsToFile(format = "csv") {
    try {
      const { data } = await logApi.post(`/api/logs/export?format=${format}`, filters.value, {
        responseType: "blob",
      });

      const now = new Date();
      const pad = (n) => n.toString().padStart(2, "0");
      const dateStr = `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}_${pad(
        now.getHours()
      )}-${pad(now.getMinutes())}-${pad(now.getSeconds())}`;

      const blobUrl = window.URL.createObjectURL(data);
      const link = document.createElement("a");
      link.href = blobUrl;
      link.download = `logs_export_${dateStr}.${format}`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(blobUrl);
    } catch (err) {
      error.value = err.response?.data?.message || err.message || "Failed to export logs";
      console.error("[LogStore] exportLogs error:", err);
    }
  }

  // ===============================
  // Filters
  // ===============================
  function setFilter(key, value) {
    filters.value[key] = value;
  }

  function clearFilters() {
    filters.value = { Layer: "", Service: "", Type: "", From: "", To: "" };
  }

  // ===============================
  // Return state & actions
  // ===============================
  return {
    logs,
    isLoading,
    error,
    filters,
    fetchLogs,
    exportLogsToFile,
    setFilter,
    clearFilters,
  };
});
