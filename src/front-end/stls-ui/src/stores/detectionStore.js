import { defineStore } from "pinia";
import { ref } from "vue";
import { intersectionClients } from "../services/httpClients";

export const useDetectionStore = defineStore("detectionStore", () => {
  // ===============================
  // State
  // ===============================
  const emergency = ref([]);
  const publicTransport = ref([]);
  const incidents = ref([]);
  const loading = ref(false);
  const error = ref(null);

  // ===============================
  // Helper to get detection API for an intersection
  // ===============================
  function getDetectionApi(intersectionKey) {
    const client = intersectionClients[intersectionKey]?.detectionApi;
    if (!client) throw new Error(`Detection API client not found for ${intersectionKey}`);
    return client;
  }

  // ===============================
  // Actions
  // ===============================
  async function loadEmergency(intersectionKey) {
    loading.value = true;
    error.value = null;
    try {
      const api = getDetectionApi(intersectionKey);
      const { data } = await api.get("/api/detections/emergency");
      emergency.value = data;
    } catch (err) {
      error.value = err.message || "Failed to load emergency detections";
      emergency.value = [];
      console.error("[DetectionStore] loadEmergency error:", err);
    } finally {
      loading.value = false;
    }
  }

  async function loadPublicTransport(intersectionKey) {
    loading.value = true;
    error.value = null;
    try {
      const api = getDetectionApi(intersectionKey);
      const { data } = await api.get("/api/detections/public-transport");
      publicTransport.value = data;
    } catch (err) {
      error.value = err.message || "Failed to load public transport detections";
      publicTransport.value = [];
      console.error("[DetectionStore] loadPublicTransport error:", err);
    } finally {
      loading.value = false;
    }
  }

  async function loadIncidents(intersectionKey) {
    loading.value = true;
    error.value = null;
    try {
      const api = getDetectionApi(intersectionKey);
      const { data } = await api.get("/api/detections/incident");
      incidents.value = data;
    } catch (err) {
      error.value = err.message || "Failed to load incident detections";
      incidents.value = [];
      console.error("[DetectionStore] loadIncidents error:", err);
    } finally {
      loading.value = false;
    }
  }

  async function loadAll(intersectionKey) {
    loading.value = true;
    error.value = null;
    try {
      const api = getDetectionApi(intersectionKey);
      const [em, pt, inc] = await Promise.all([
        api.get("/api/detections/emergency").then(res => res.data),
        api.get("/api/detections/public-transport").then(res => res.data),
        api.get("/api/detections/incident").then(res => res.data),
      ]);
      emergency.value = em;
      publicTransport.value = pt;
      incidents.value = inc;
    } catch (err) {
      error.value = err.message || "Failed to load detections";
      emergency.value = [];
      publicTransport.value = [];
      incidents.value = [];
      console.error("[DetectionStore] loadAll error:", err);
    } finally {
      loading.value = false;
    }
  }

  // ===============================
  // Return state & actions
  // ===============================
  return {
    emergency,
    publicTransport,
    incidents,
    loading,
    error,
    loadEmergency,
    loadPublicTransport,
    loadIncidents,
    loadAll,
  };
});
