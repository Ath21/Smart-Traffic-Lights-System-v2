<template>
  <!-- Splash Screen: only on app bootstrap -->
  <div
    v-if="!auth.ready"
    class="fixed inset-0 flex flex-col items-center justify-center bg-[#002b5c] text-white z-50"
  >
    <img
      src="/STLS-Logo.png"
      alt="STLS Logo"
      class="w-32 h-32 mb-4 animate-pulse"
    />
    <h2 class="text-xl font-semibold tracking-wide">
      Loading UNIWA STLS...
    </h2>
  </div>

  <!-- Main App Layout -->
  <div v-else class="flex flex-col min-h-screen bg-white">
    <!-- Fixed top bar -->
    <TopBar v-if="showTopbar" class="sticky top-0 z-50" />

    <!-- Page content -->
    <main class="flex-1 overflow-hidden">
      <RouterView />
    </main>
  </div>
</template>

<script setup>
import { computed } from "vue";
import { useRoute } from "vue-router";
import { useUserStore } from "./stores/userStore";
import TopBar from "./components/TopBar.vue";

const auth = useUserStore();
const route = useRoute();

// Pages where topbar is shown
const topbarPages = [
  "/",
  "/stls",
  "/login",
  "/register",
  "/reset-password",
  "/stls/profile",
  "/stls/update",
  "/stls/subscriptions",
  "/stls/subscribe",
  "/stls/admin",
  "/stls/users",
  "/stls/audits",
  "/stls/analytics",
  "/stls/intersection/agiou-spyridonos",
  "/stls/intersection/anatoliki-pyli",
  "/stls/intersection/dytiki-pyli",
  "/stls/intersection/ekklisia",
  "/stls/intersection/kentriki-pyli",
];

const showTopbar = computed(() => topbarPages.includes(route.path));
</script>

<style scoped>
@keyframes pulse {
  0%,
  100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.7;
    transform: scale(1.1);
  }
}
</style>
