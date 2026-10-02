import { coordinatorApi } from "./httpClients"; // preferred central client

// === Intersections ===
export const getAllIntersections = () =>
  coordinatorApi.get("/api/intersections/all");

export const getIntersectionById = (id) =>
  coordinatorApi.get(`/api/intersections/${id}`);

// === Traffic Configurations ===
export const getAllConfigurations = () =>
  coordinatorApi.get("/api/configurations/all");

export const getConfigurationByMode = (mode) =>
  coordinatorApi.get("/api/configurations/mode", { params: { mode } });

// === Traffic Lights ===
export const getAllTrafficLights = () =>
  coordinatorApi.get("/api/traffic-lights/all");

export const getTrafficLightById = (id) =>
  coordinatorApi.get(`/api/traffic-lights/${id}`);

// === Traffic Operator ===
export const applyTrafficMode = (data) =>
  coordinatorApi.post("/api/traffic-operator/apply-mode", data);

export const overrideTrafficLight = (data) =>
  coordinatorApi.post("/api/traffic-operator/override-light", data);

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