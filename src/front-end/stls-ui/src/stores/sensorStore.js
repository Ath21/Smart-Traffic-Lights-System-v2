import { defineStore } from "pinia";
import {
  fetchVehicleSensors,
  fetchPedestrianSensors,
  fetchCyclistSensors,
} from "../services/sensorApi";

export const useSensorStore = defineStore("sensorStore", {
  state: () => ({
    vehicle: [],
    pedestrian: [],
    cyclist: [],
    loading: false,
    error: null,
  }),

  actions: {
    async loadVehicle(intersectionKey) {
      this.loading = true;
      this.error = null;
      try {
        this.vehicle = await fetchVehicleSensors(intersectionKey);
      } catch (err) {
        this.error = err.message || "Failed to load vehicle sensor data";
      } finally {
        this.loading = false;
      }
    },

    async loadPedestrian(intersectionKey) {
      this.loading = true;
      this.error = null;
      try {
        this.pedestrian = await fetchPedestrianSensors(intersectionKey);
      } catch (err) {
        this.error = err.message || "Failed to load pedestrian sensor data";
      } finally {
        this.loading = false;
      }
    },

    async loadCyclist(intersectionKey) {
      this.loading = true;
      this.error = null;
      try {
        this.cyclist = await fetchCyclistSensors(intersectionKey);
      } catch (err) {
        this.error = err.message || "Failed to load cyclist sensor data";
      } finally {
        this.loading = false;
      }
    },

    // Optional: load all sensor types at once
    async loadAll(intersectionKey) {
      this.loading = true;
      this.error = null;
      try {
        const [vehicle, pedestrian, cyclist] = await Promise.all([
          fetchVehicleSensors(intersectionKey),
          fetchPedestrianSensors(intersectionKey),
          fetchCyclistSensors(intersectionKey),
        ]);
        this.vehicle = vehicle;
        this.pedestrian = pedestrian;
        this.cyclist = cyclist;
      } catch (err) {
        this.error = err.message || "Failed to load sensor data";
      } finally {
        this.loading = false;
      }
    },
  },
});
