import { defineStore } from "pinia";
import { ref } from "vue";
import { coordinatorApi } from "../services/httpClients";

export const useCoordinatorStore = defineStore("coordinatorStore", () => {
  // ===============================
  // State
  // ===============================
  const intersections = ref([]);
  const selectedIntersection = ref(null);
  const configurations = ref([]);
  const currentConfiguration = ref(null); // <-- added
  const trafficLights = ref([]);
  const loading = ref(false);
  const error = ref(null);

  // ===============================
  // Intersections
  // ===============================
  async function fetchIntersections() {
    loading.value = true;
    error.value = null;
    try {
      const res = await coordinatorApi.get("/api/intersections/all");
      intersections.value = res.data;
    } catch (err) {
      error.value =
        err.response?.status
          ? `[HTTP ${err.response.status}]`
          : err.message || "Failed to load intersections.";
      intersections.value = [];
      console.error("[CoordinatorStore] fetchIntersections error:", err);
    } finally {
      loading.value = false;
    }
  }

  async function selectIntersection(id) {
    loading.value = true;
    error.value = null;
    try {
      const res = await coordinatorApi.get(`/api/intersections/${id}`);
      selectedIntersection.value = res.data;
    } catch (err) {
      error.value =
        err.response?.status
          ? `[HTTP ${err.response.status}]`
          : err.message || "Failed to fetch intersection details.";
      selectedIntersection.value = null;
      console.error("[CoordinatorStore] selectIntersection error:", err);
    } finally {
      loading.value = false;
    }
  }

  // ===============================
  // Configurations
  // ===============================
  async function fetchConfigurations() {
    loading.value = true;
    error.value = null;
    try {
      const res = await coordinatorApi.get("/api/configurations/all");
      configurations.value = res.data;
    } catch (err) {
      error.value =
        err.response?.status
          ? `[HTTP ${err.response.status}]`
          : err.message || "Failed to fetch configurations.";
      configurations.value = [];
      console.error("[CoordinatorStore] fetchConfigurations error:", err);
    } finally {
      loading.value = false;
    }
  }

  // Fetch configuration by mode and set currentConfiguration
  async function fetchConfigurationByMode(mode) {
    loading.value = true;
    error.value = null;
    try {
      const res = await coordinatorApi.get("/api/configurations/mode", {
        params: { mode },
      });

      configurations.value = res.data;

      // set currentConfiguration to the API result
      currentConfiguration.value = res.data;
    } catch (err) {
      error.value =
        err.response?.status
          ? `[HTTP ${err.response.status}]`
          : err.message || `Failed to fetch configuration for mode "${mode}".`;
      console.error("[CoordinatorStore] fetchConfigurationByMode error:", err);
      currentConfiguration.value = null;
    } finally {
      loading.value = false;
    }
  }

  // ===============================
  // Traffic Lights
  // ===============================
  async function fetchTrafficLights() {
    loading.value = true;
    error.value = null;
    try {
      const res = await coordinatorApi.get("/api/traffic-lights/all");
      trafficLights.value = res.data;
    } catch (err) {
      error.value =
        err.response?.status
          ? `[HTTP ${err.response.status}]`
          : err.message || "Failed to fetch traffic lights.";
      trafficLights.value = [];
      console.error("[CoordinatorStore] fetchTrafficLights error:", err);
    } finally {
      loading.value = false;
    }
  }

  // ===============================
  // Traffic Operator
  // ===============================
  async function applyMode(intersectionId, mode) {
    loading.value = true;
    error.value = null;
    try {
      await coordinatorApi.post("/api/traffic-operator/apply-mode", {
        IntersectionId: intersectionId,
        Mode: mode,
      });
    } catch (err) {
      error.value =
        err.response?.status
          ? `[HTTP ${err.response.status}]`
          : err.message || "Failed to apply traffic mode.";
      console.error("[CoordinatorStore] applyMode error:", err);
    } finally {
      loading.value = false;
    }
  }

  async function overrideLight(data) {
    loading.value = true;
    error.value = null;
    try {
      await coordinatorApi.post("/api/traffic-operator/override-light", data);
    } catch (err) {
      error.value =
        err.response?.status
          ? `[HTTP ${err.response.status}]`
          : err.message || "Failed to override light.";
      console.error("[CoordinatorStore] overrideLight error:", err);
    } finally {
      loading.value = false;
    }
  }

  // ===============================
  // Return state & actions
  // ===============================
  return {
    intersections,
    selectedIntersection,
    configurations,
    currentConfiguration, // <-- expose currentConfiguration
    trafficLights,
    loading,
    error,
    fetchIntersections,
    selectIntersection,
    fetchConfigurations,
    fetchConfigurationByMode,
    fetchTrafficLights,
    applyMode,
    overrideLight,
  };
});
