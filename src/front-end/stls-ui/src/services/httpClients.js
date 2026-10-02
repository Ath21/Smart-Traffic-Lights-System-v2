// src/services/httpClients.js
import axios from "axios";
import { decodeToken } from "../utils/jwtHelper";

/**
 * Creates an authenticated Axios client.
 * @param {string} baseURL - API base URL.
 * @returns {import('axios').AxiosInstance}
 */
function createHttpClient(baseURL) {
  const client = axios.create({
    baseURL,
    headers: { "Content-Type": "application/json" },
  });

  // === Request Interceptor for Authorization ===
  client.interceptors.request.use(
    (config) => {
      const token = localStorage.getItem("stls_token");
      if (token) {
        const decoded = decodeToken(token);
        const now = Math.floor(Date.now() / 1000);
        if (decoded && decoded.exp > now) {
          config.headers.Authorization = `Bearer ${token}`;
        } else {
          console.warn("[httpClients] JWT expired or invalid, skipping header");
        }
      }
      return config;
    },
    (error) => Promise.reject(error)
  );

  // === Response Interceptor (optional logging) ===
  client.interceptors.response.use(
    (res) => res,
    (err) => {
      console.error("[HTTP Error]", err.response?.status, err.message);
      return Promise.reject(err);
    }
  );

  return client;
}

// ===============================================
// Local Override URLs (for local development)
// ===============================================
const OVERRIDES = {
  // Core Services
  VITE_USER_API: "http://localhost:5055",
  VITE_NOTIFICATION_API: "http://localhost:5087",
  VITE_LOG_API: "http://localhost:5005",
  VITE_TRAFFIC_ANALYTICS: "http://localhost:5208", // keep typo key
  VITE_TRAFFIC_LIGHT_COORDINATOR: "http://localhost:5020",

  // Agiou Spyridonos
  VITE_DETECTION_API_AGIOU_SPYRIDONOS: "http://localhost:5122",
  VITE_SENSOR_API_AGIOU_SPYRIDONOS: "http://localhost:5070",
  VITE_INTERSECTION_CONTROLLER_AGIOU_SPYRIDONOS: "http://localhost:5190",
  VITE_TRAFFIC_LIGHT_CONTROLLER_AGIOU_SPYRIDONOS101: "http://localhost:5261",
  VITE_TRAFFIC_LIGHT_CONTROLLER_DIMITSANAS102: "http://localhost:5262",

  // Anatoliki Pyli
  VITE_DETECTION_API_ANATOLIKI_PYLI: "http://localhost:5123",
  VITE_SENSOR_API_ANATOLIKI_PYLI: "http://localhost:5071",
  VITE_INTERSECTION_CONTROLLER_ANATOLIKI_PYLI: "http://localhost:5191",
  VITE_TRAFFIC_LIGHT_CONTROLLER_AGIOU_SPYRIDONOS201: "http://localhost:5264",
  VITE_TRAFFIC_LIGHT_CONTROLLER_ANATOLIKI_PYLI202: "http://localhost:5263",

  // Dytiki Pyli
  VITE_DETECTION_API_DYTIKI_PYLI: "http://localhost:5124",
  VITE_SENSOR_API_DYTIKI_PYLI: "http://localhost:5072",
  VITE_INTERSECTION_CONTROLLER_DYTIKI_PYLI: "http://localhost:5192",
  VITE_TRAFFIC_LIGHT_CONTROLLER_DYTIKI_PYLI301: "http://localhost:5267",
  VITE_TRAFFIC_LIGHT_CONTROLLER_DIMITSANAS_NORTH302: "http://localhost:5265",
  VITE_TRAFFIC_LIGHT_CONTROLLER_DIMITSANAS_SOUTH303: "http://localhost:5266",

  // Ekklisia
  VITE_DETECTION_API_EKKLISIA: "http://localhost:5125",
  VITE_SENSOR_API_EKKLISIA: "http://localhost:5073",
  VITE_INTERSECTION_CONTROLLER_EKKLISIA: "http://localhost:5193",
  VITE_TRAFFIC_LIGHT_CONTROLLER_DIMITSANAS401: "http://localhost:5268",
  VITE_TRAFFIC_LIGHT_CONTROLLER_EDESSIS402: "http://localhost:5269",
  VITE_TRAFFIC_LIGHT_CONTROLLER_KORYTSAS403: "http://localhost:5270",

  // Kentriki Pyli
  VITE_DETECTION_API_KENTRIKI_PYLI: "http://localhost:5126",
  VITE_SENSOR_API_KENTRIKI_PYLI: "http://localhost:5074",
  VITE_INTERSECTION_CONTROLLER_KENTRIKI_PYLI: "http://localhost:5194",
  VITE_TRAFFIC_LIGHT_CONTROLLER_KENTRIKI_PYLI501: "http://localhost:5272",
  VITE_TRAFFIC_LIGHT_CONTROLLER_AGIOU_SPYRIDONOS502: "http://localhost:5271",
};

// ===============================================
// Core Service Clients
// ===============================================
export const userApi = createHttpClient(OVERRIDES.VITE_USER_API);
export const notificationApi = createHttpClient(OVERRIDES.VITE_NOTIFICATION_API);
export const logApi = createHttpClient(OVERRIDES.VITE_LOG_API);
export const analyticsApi = createHttpClient(OVERRIDES.VITE_TRAFFIC_ANALYTICS);
export const coordinatorApi = createHttpClient(OVERRIDES.VITE_TRAFFIC_LIGHT_COORDINATOR);

// ===============================================
// Intersection-Specific Clients (Edge/Fog nodes)
// ===============================================
export const intersectionClients = {
  "AGIOU-SPYRIDONOS": {
    detectionApi: createHttpClient(OVERRIDES.VITE_DETECTION_API_AGIOU_SPYRIDONOS),
    sensorApi: createHttpClient(OVERRIDES.VITE_SENSOR_API_AGIOU_SPYRIDONOS),
    intersectionController: createHttpClient(OVERRIDES.VITE_INTERSECTION_CONTROLLER_AGIOU_SPYRIDONOS),
    lightControllers: {
      AGIOU_SPYRIDONOS_101: createHttpClient(OVERRIDES.VITE_TRAFFIC_LIGHT_CONTROLLER_AGIOU_SPYRIDONOS101),
      DIMITSANAS_102: createHttpClient(OVERRIDES.VITE_TRAFFIC_LIGHT_CONTROLLER_DIMITSANAS102),
    },
  },

  "ANATOLIKI-PYLI": {
    detectionApi: createHttpClient(OVERRIDES.VITE_DETECTION_API_ANATOLIKI_PYLI),
    sensorApi: createHttpClient(OVERRIDES.VITE_SENSOR_API_ANATOLIKI_PYLI),
    intersectionController: createHttpClient(OVERRIDES.VITE_INTERSECTION_CONTROLLER_ANATOLIKI_PYLI),
    lightControllers: {
      AGIOU_SPYRIDONOS_201: createHttpClient(OVERRIDES.VITE_TRAFFIC_LIGHT_CONTROLLER_AGIOU_SPYRIDONOS201),
      ANATOLIKI_PYLI_202: createHttpClient(OVERRIDES.VITE_TRAFFIC_LIGHT_CONTROLLER_ANATOLIKI_PYLI202),
    },
  },

  "DYTIKI-PYLI": {
    detectionApi: createHttpClient(OVERRIDES.VITE_DETECTION_API_DYTIKI_PYLI),
    sensorApi: createHttpClient(OVERRIDES.VITE_SENSOR_API_DYTIKI_PYLI),
    intersectionController: createHttpClient(OVERRIDES.VITE_INTERSECTION_CONTROLLER_DYTIKI_PYLI),
    lightControllers: {
      DYTIKI_PYLI_301: createHttpClient(OVERRIDES.VITE_TRAFFIC_LIGHT_CONTROLLER_DYTIKI_PYLI301),
      DIMITSANAS_NORTH_302: createHttpClient(OVERRIDES.VITE_TRAFFIC_LIGHT_CONTROLLER_DIMITSANAS_NORTH302),
      DIMITSANAS_SOUTH_303: createHttpClient(OVERRIDES.VITE_TRAFFIC_LIGHT_CONTROLLER_DIMITSANAS_SOUTH303),
    },
  },

  "EKKLISIA": {
    detectionApi: createHttpClient(OVERRIDES.VITE_DETECTION_API_EKKLISIA),
    sensorApi: createHttpClient(OVERRIDES.VITE_SENSOR_API_EKKLISIA),
    intersectionController: createHttpClient(OVERRIDES.VITE_INTERSECTION_CONTROLLER_EKKLISIA),
    lightControllers: {
      DIMITSANAS_401: createHttpClient(OVERRIDES.VITE_TRAFFIC_LIGHT_CONTROLLER_DIMITSANAS401),
      EDESSIS_402: createHttpClient(OVERRIDES.VITE_TRAFFIC_LIGHT_CONTROLLER_EDESSIS402),
      KORYTSAS_403: createHttpClient(OVERRIDES.VITE_TRAFFIC_LIGHT_CONTROLLER_KORYTSAS403),
    },
  },

  "KENTRIKI-PYLI": {
    detectionApi: createHttpClient(OVERRIDES.VITE_DETECTION_API_KENTRIKI_PYLI),
    sensorApi: createHttpClient(OVERRIDES.VITE_SENSOR_API_KENTRIKI_PYLI),
    intersectionController: createHttpClient(OVERRIDES.VITE_INTERSECTION_CONTROLLER_KENTRIKI_PYLI),
    lightControllers: {
      KENTRIKI_PYLI_501: createHttpClient(OVERRIDES.VITE_TRAFFIC_LIGHT_CONTROLLER_KENTRIKI_PYLI501),
      AGIOU_SPYRIDONOS_502: createHttpClient(OVERRIDES.VITE_TRAFFIC_LIGHT_CONTROLLER_AGIOU_SPYRIDONOS502),
    },
  },
};
