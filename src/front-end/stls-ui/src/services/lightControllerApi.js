import { intersectionClients } from "./httpClients";

/**
 * Helper: get Axios client for a specific light ID
 * Looks up the correct controller from the centralized clients
 */
function getLightApi(lightId) {
  // Flatten all controllers from all intersections
  const controllers = Object.values(intersectionClients).flatMap(inter =>
    Object.entries(inter.lightControllers).map(([key, client]) => ({ key, client }))
  );

  // Find the client whose key ends with the lightId
  const entry = controllers.find(({ key }) => key.endsWith(lightId.toString()));
  if (!entry) {
    throw new Error(`[lightControllerApi] No controller client found for light ID ${lightId}`);
  }

  return entry.client;
}

// === API Methods ===

// Get operational state (current color, timers)
export const getLightState = (lightId) => {
  const client = getLightApi(lightId);
  return client.get("/api/traffic-lights/state", { params: { id: lightId } });
};

// Get full light cycle configuration (green/yellow/red durations)
export const getLightCycle = (lightId) => {
  const client = getLightApi(lightId);
  return client.get("/api/traffic-lights/cycle", { params: { id: lightId } });
};

// Get current failover/fallback mode state (manual/auto)
export const getLightFailover = (lightId) => {
  const client = getLightApi(lightId);
  return client.get("/api/traffic-lights/failover", { params: { id: lightId } });
};

// Optional bulk fetch helper
export async function getAllLightStates() {
  const results = [];
  const lightIds = Object.values(intersectionClients)
    .flatMap(inter => Object.keys(inter.controllers));

  for (const id of lightIds) {
    try {
      const res = await getLightState(id);
      results.push({ id, ...res.data });
    } catch (err) {
      results.push({ id, error: err.message });
    }
  }

  return results;
}

// ===============================
// HEALTH CHECKS
// ===============================

export async function checkHealth() {
  const { data } = await userApi.get("/health");
  return data;
}

export async function checkReady() {
  const { data } = await userApi.get("/ready");
  return data;
}

export default {
  getLightState,
  getLightCycle,
  getLightFailover,
  getAllLightStates
};
