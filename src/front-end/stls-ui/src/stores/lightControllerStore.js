import { defineStore } from "pinia";
import { ref } from "vue";
import { intersectionClients } from "../services/httpClients";

export const useLightControllerStore = defineStore("lightControllerStore", () => {
  // ===============================
  // State
  // ===============================
  const selectedLight = ref(null);
  const lightsData = ref({}); // holds all lights keyed by id
  const loading = ref(false);
  const error = ref(null);

  // ===============================
  // Helper: get Axios client for a light
  // ===============================
  function getLightClient(lightId) {
    for (const intersection of Object.values(intersectionClients)) {
      if (!intersection.lightControllers) continue;

      for (const [key, client] of Object.entries(intersection.lightControllers)) {
        // Match by LightId at the end of key
        if (key.endsWith(lightId.toString())) {
          return client;
        }
      }
    }
    throw new Error(`[LightControllerStore] No controller client found for light ID ${lightId}`);
  }


  // ===============================
  // Fetch a single light's full data (state + cycle + failover)
  // ===============================
  async function fetchLightData(lightId) {
    loading.value = true;
    error.value = null;

    try {
      const client = getLightClient(lightId);

      const [stateRes, cycleRes, failoverRes] = await Promise.all([
        client.get("/api/traffic-lights/state", { params: { id: lightId } }),
        client.get("/api/traffic-lights/cycle", { params: { id: lightId } }),
        client.get("/api/traffic-lights/failover", { params: { id: lightId } }),
      ]);

      const lightData = {
        state: stateRes.data,
        cycle: cycleRes.data,
        failover: failoverRes.data,
      };

      lightsData.value[lightId] = lightData;

      // Update selectedLight data if needed
      if (selectedLight.value?.id === lightId) {
        selectedLight.value.state = lightData.state;
        selectedLight.value.cycle = lightData.cycle;
        selectedLight.value.failover = lightData.failover;
      }

      return lightData;
    } catch (err) {
      error.value = err.response?.status
        ? `[HTTP ${err.response.status}]`
        : err.message || "Failed to fetch light data.";
      console.error("[LightControllerStore] fetchLightData error:", err);
      return null;
    } finally {
      loading.value = false;
    }
  }

  // ===============================
  // Fetch multiple lights in parallel
  // ===============================
  async function fetchLightsForIntersection(lights) {
    if (!lights?.length) return {};
    loading.value = true;
    error.value = null;
console.log("LightsData after fetch:", lightsData.value);

    try {
      const results = await Promise.all(
        lights.map((light) => fetchLightData(light.LightId))
      );

      return results.reduce((acc, data, idx) => {
        if (data) acc[lights[idx].LightId] = data;
        return acc;
      }, {});
    } catch (err) {
      error.value = err.response?.status
        ? `[HTTP ${err.response.status}]`
        : err.message || "Failed to fetch lights for intersection.";
      console.error("[LightControllerStore] fetchLightsForIntersection error:", err);
      return {};
    } finally {
      loading.value = false;
    }
  }


  // ===============================
  // Select a light
  // ===============================
  async function selectLight(light) {
    selectedLight.value = light;
    await fetchLightData(light.id);
  }

  // ===============================
  // Return state & actions
  // ===============================
  return {
    selectedLight,
    lightsData,
    loading,
    error,
    selectLight,
    fetchLightData,
    fetchLightsForIntersection,
  };
});
