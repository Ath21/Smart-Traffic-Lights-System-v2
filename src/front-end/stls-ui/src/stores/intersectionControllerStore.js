import { defineStore } from "pinia";
import { ref } from "vue";
import { checkHealth, checkReady } from "../services/intersectionControllerApi";

export const useIntersectionControllerStore = defineStore("intersectionControllerStore", () => {
  const healthStatus = ref({});
  const readyStatus = ref({});
  const loading = ref(false);
  const error = ref(null);

  // Fetch health for a given intersection
  async function fetchHealth(intersectionKey) {
    loading.value = true;
    error.value = null;
    try {
      const data = await checkHealth(intersectionKey);
      healthStatus.value[intersectionKey] = data;
    } catch (err) {
      error.value = err.message || `Failed to fetch health for ${intersectionKey}`;
      healthStatus.value[intersectionKey] = null;
      console.error("[IntersectionControllerStore] fetchHealth error:", err);
    } finally {
      loading.value = false;
    }
  }

  // Fetch ready for a given intersection
  async function fetchReady(intersectionKey) {
    loading.value = true;
    error.value = null;
    try {
      const data = await checkReady(intersectionKey);
      readyStatus.value[intersectionKey] = data;
    } catch (err) {
      error.value = err.message || `Failed to fetch ready for ${intersectionKey}`;
      readyStatus.value[intersectionKey] = null;
      console.error("[IntersectionControllerStore] fetchReady error:", err);
    } finally {
      loading.value = false;
    }
  }

  // Fetch both health and ready in parallel
  async function fetchAll(intersectionKey) {
    loading.value = true;
    error.value = null;
    try {
      const [health, ready] = await Promise.all([
        checkHealth(intersectionKey),
        checkReady(intersectionKey)
      ]);
      healthStatus.value[intersectionKey] = health;
      readyStatus.value[intersectionKey] = ready;
    } catch (err) {
      error.value = err.message || `Failed to fetch statuses for ${intersectionKey}`;
      healthStatus.value[intersectionKey] = null;
      readyStatus.value[intersectionKey] = null;
      console.error("[IntersectionControllerStore] fetchAll error:", err);
    } finally {
      loading.value = false;
    }
  }

  return {
    healthStatus,
    readyStatus,
    loading,
    error,
    fetchHealth,
    fetchReady,
    fetchAll
  };
});
