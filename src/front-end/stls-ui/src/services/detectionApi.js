import { intersectionClients } from "./httpClients";

/**
 * Get the Axios client for a given intersection
 * @param {string} intersectionKey - e.g. "KENTRIKI-PYLI"
 */
function getDetectionClient(intersectionKey) {
  const client = intersectionClients[intersectionKey]?.detectionApi;
  if (!client) throw new Error(`No detection API client found for intersection "${intersectionKey}"`);
  return client;
}

// === Detection Endpoints ===

export async function fetchEmergencyDetections(intersectionKey) {
  const client = getDetectionClient(intersectionKey);
  const { data } = await client.get("/api/detections/emergency");
  return data;
}

export async function fetchPublicTransportDetections(intersectionKey) {
  const client = getDetectionClient(intersectionKey);
  const { data } = await client.get("/api/detections/public-transport");
  return data;
}

export async function fetchIncidentDetections(intersectionKey) {
  const client = getDetectionClient(intersectionKey);
  const { data } = await client.get("/api/detections/incident");
  return data;
}

// === Health / Ready ===
export async function checkDetectionHealth(intersectionKey) {
  const client = getDetectionClient(intersectionKey);
  const { data } = await client.get("/health");
  return data;
}

export async function checkDetectionReady(intersectionKey) {
  const client = getDetectionClient(intersectionKey);
  const { data } = await client.get("/ready");
  return data;
}
