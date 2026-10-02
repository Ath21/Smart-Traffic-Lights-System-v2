import { intersectionClients } from "./httpClients";

/**
 * Get Axios client for a specific intersection
 * @param {string} intersectionKey
 */
function getControllerClient(intersectionKey) {
  const client = intersectionClients[intersectionKey]?.intersectionController;
  if (!client) throw new Error(`Intersection Controller API not found for ${intersectionKey}`);
  return client;
}

// === Health / Ready checks ===
export async function checkHealth(intersectionKey) {
  const api = getControllerClient(intersectionKey);
  const { data } = await api.get("/health");
  return data;
}

export async function checkReady(intersectionKey) {
  const api = getControllerClient(intersectionKey);
  const { data } = await api.get("/ready");
  return data;
}
