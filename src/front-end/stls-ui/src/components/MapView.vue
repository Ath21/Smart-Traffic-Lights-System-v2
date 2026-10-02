<template>
  <div class="map-wrapper">
    <!-- === Map Layer === -->
    <div ref="mapEl" class="leaflet-wrapper"></div>

    <!-- === Legend (Bottom Left) === -->
    <div class="legend-box">
      <div><span class="traffic-light red"></span> Stop</div>
      <div><span class="traffic-light yellow"></span> Caution</div>
      <div><span class="traffic-light green"></span> Go</div>
      <div><span class="traffic-light orange"></span> Out of Service</div>
      <hr class="border-t border-gray-500 my-1">
      <div><span class="legend-icon low"></span> Low Congestion</div>
      <div><span class="legend-icon medium"></span> Medium Congestion</div>
      <div><span class="legend-icon high"></span> High Congestion</div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, onBeforeUnmount, ref } from "vue";
import L from "leaflet";
import "../assets/map.css";
import "../assets/legend-lights.css";
import { useLightControllerStore } from "../stores/lightControllerStore";

const mapEl = ref(null);
let map, group, intervalId;

const lightStore = useLightControllerStore();

// ===============================
// Intersections & lights
// ===============================
const intersections = [
  {
    id: 4,
    name: "Ekklisia",
    center: [38.00158, 23.673638],
    lights: [
      { LightId: 401, label: "Dimitsanas", coords: [38.001626, 23.673627] },
      { LightId: 402, label: "Edessis", coords: [38.001583, 23.673566] },
      { LightId: 403, label: "Korytsas", coords: [38.001596, 23.673686] }
    ]
  },
  {
    id: 3,
    name: "Dytiki Pyli",
    center: [38.002644, 23.674499],
    lights: [
      { LightId: 301, label: "Dytiki Pyli", coords: [38.002648, 23.674531] },
      { LightId: 303, label: "Dimitsanas South", coords: [38.002606, 23.674487] },
      { LightId: 302, label: "Dimitsanas North", coords: [38.002696, 23.674498] }
    ]
  },
  {
    id: 1,
    name: "Agiou Spyridonos",
    center: [38.004677, 23.676086],
    lights: [
      { LightId: 102, label: "Dimitsanas", coords: [38.00464, 23.676094] },
      { LightId: 101, label: "Agiou Spyridonos", coords: [38.004685, 23.676139] }
    ]
  },
  {
    id: 5,
    name: "Kentriki Pyli",
    center: [38.004456, 23.676483],
    lights: [
      { LightId: 501, label: "Kentriki Pyli", coords: [38.004447, 23.676453] },
      { LightId: 502, label: "Agiou Spyridonos", coords: [38.004467, 23.676528] }
    ]
  },
  {
    id: 2,
    name: "Anatoliki Pyli",
    center: [38.003558, 23.678042],
    lights: [
      { LightId: 201, label: "Anatoliki Pyli", coords: [38.003549, 23.677997] },
      { LightId: 202, label: "Agiou Spyridonos", coords: [38.00357, 23.678093] }
    ]
  }
];

// ===============================
// Helpers
// ===============================
function trafficIcon(state = "red") {
  return L.divIcon({
    html: `<div class="traffic-light ${state.toLowerCase()}"></div>`,
    className: "",
    iconSize: [20, 20],
    iconAnchor: [10, 10],
    popupAnchor: [0, -10]
  });
}

// Safely get live state from backend
function safeState(lightId) {
  const s = lightStore.lightsData[lightId]?.state;
  if (!s) return "red";
  return (s.Phase || "Red").toLowerCase();
}

// Determine congestion from live states
function getCongestion(inter) {
  const redCount = inter.lights.filter(
    l => safeState(l.LightId) === "red"
  ).length;
  const total = inter.lights.length;
  const ratio = redCount / total;

  if (ratio === 0) return "low";
  if (ratio < 0.5) return "medium";
  return "high";
}

// Render map with live markers and labels
function renderMap() {
  if (!group) return;
  group.clearLayers();

  intersections.forEach(inter => {
    inter.lights.forEach(light => {
      const liveState = safeState(light.LightId);

      L.marker(light.coords, { icon: trafficIcon(liveState) })
        .addTo(group)
        .bindPopup(
          `<b>${inter.name}</b><br/>
           [${light.LightId}] ${light.label}<br/>
           State: <b>${liveState.toUpperCase()}</b><br/>
           Mode: ${lightStore.lightsData[light.LightId]?.state?.Mode || '-'}<br/>
           Remaining: ${lightStore.lightsData[light.LightId]?.state?.Remaining || '-'}`
        );
    });

    // Intersection center circle
    L.circle(inter.center, {
      radius: 20,
      color: "blue",
      fillColor: "#3f82ff",
      fillOpacity: 0.1
    }).addTo(group);

    // Intersection label with congestion
    const congestion = getCongestion(inter);
    L.tooltip({
      permanent: true,
      direction: "top",
      offset: [0, -25],
      className: `intersection-label ${congestion}`
    })
      .setContent(`<b>[${inter.id}] ${inter.name}</b>`)
      .setLatLng(inter.center)
      .addTo(group);
  });
}

// ===============================
// Live polling
// ===============================
async function pollLights() {
  if (!intersections?.length) return;

  for (const inter of intersections) {
    await lightStore.fetchLightsForIntersection(inter.lights);
  }

  renderMap();
}

function startPolling() {
  pollLights(); // initial fetch
  intervalId = setInterval(pollLights, 5000); // every 5s
}

function stopPolling() {
  if (intervalId) clearInterval(intervalId);
}

// ===============================
// Lifecycle
// ===============================
onMounted(() => {
  map = L.map(mapEl.value, { zoomControl: true }).setView([38.0035, 23.675], 16);

  L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
    maxZoom: 20,
    attribution: "&copy; OpenStreetMap contributors"
  }).addTo(map);

  group = L.featureGroup().addTo(map);
  map.fitBounds(L.latLngBounds(intersections.map(i => i.center)), { padding: [40, 40] });

  setTimeout(() => map.invalidateSize(), 400);

  startPolling();
});

onBeforeUnmount(() => {
  stopPolling();
  map?.remove();
});
</script>
