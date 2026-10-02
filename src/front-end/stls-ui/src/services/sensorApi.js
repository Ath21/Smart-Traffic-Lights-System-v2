import { intersectionClients } from "./httpClients";

/**
 * Get the Axios client for a given intersection
 * @param {string} intersectionKey - e.g. "KENTRIKI-PYLI"
 */
function getSensorClient(intersectionKey) {
  const client = intersectionClients[intersectionKey]?.sensorApi;
  if (!client) throw new Error(`No sensor API client found for intersection "${intersectionKey}"`);
  return client;
}

// === Sensor Endpoints ===

export async function fetchVehicleSensors(intersectionKey) {
  const client = getSensorClient(intersectionKey);
  const { data } = await client.get("/api/sensors/vehicle");
  return data;
}

export async function fetchPedestrianSensors(intersectionKey) {
  const client = getSensorClient(intersectionKey);
  const { data } = await client.get("/api/sensors/pedestrian");
  return data;
}

export async function fetchCyclistSensors(intersectionKey) {
  const client = getSensorClient(intersectionKey);
  const { data } = await client.get("/api/sensors/cyclist");
  return data;
}

// === Health / Ready ===
export async function checkSensorHealth(intersectionKey) {
  const client = getSensorClient(intersectionKey);
  const { data } = await client.get("/health");
  return data;
}

export async function checkSensorReady(intersectionKey) {
  const client = getSensorClient(intersectionKey);
  const { data } = await client.get("/ready");
  return data;
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