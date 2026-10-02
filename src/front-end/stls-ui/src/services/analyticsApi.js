import { analyticsApi } from "./httpClients"; // centralized client

// === Analytics API ===

// Fetch traffic summaries
export const fetchSummariesApi = (params = {}) =>
  analyticsApi.get("/api/analytics/summaries", { params });

// Fetch traffic alerts
export const fetchAlertsApi = (params = {}) =>
  analyticsApi.get("/api/analytics/alerts", { params });

export const exportSummariesCsvApi = (params = {}) =>
  analyticsApi.get("/api/analytics/summaries/export", {
    params,
    responseType: "blob" // ensures CSV file download
  });

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