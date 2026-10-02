<template>
  <header class="topbar">
    <!-- Left side: Logos & Title -->
    <div class="left">
      <img src="/PADA.png" alt="PADA" class="pada-logo" />
      <h1 class="title">
        UNIWA <span class="highlight">STLS</span>
      </h1>
      <img src="/STLS-Logo-TopBar.png" alt="UNIWA STLS" class="stls-logo" />
    </div>

    <!-- Middle Live Alerts -->
    <div v-if="latestAlert" class="middle-alerts">
      <span class="alert-badge">Alert</span>
      <div class="alert-text">
        <strong>{{ latestAlert.Type }}</strong> —
        {{ latestAlert.Message }} at
        <strong>{{ latestAlert.Intersection }}</strong>
      </div>
    </div>

    <!-- Right side: Role-based Buttons / User Menu -->
    <div class="right-nav">
      <!-- Not logged in -->
      <template v-if="!auth.isAuthenticated">
        <RouterLink to="/" class="btn">Home</RouterLink>
        <RouterLink to="/login" class="btn-outline">Login</RouterLink>
        <RouterLink to="/register" class="btn-primary">Register</RouterLink>
      </template>

      <!-- Logged in: User -->
      <template v-else-if="auth.user.role === 'User'">
        <RouterLink to="/stls" class="btn">Home</RouterLink>
        <RouterLink to="/stls/analytics" class="btn-outline">Analytics</RouterLink>
        <RouterLink to="/stls/subscribe" class="btn-outline">Alert Me</RouterLink>

        <UserMenu
          :username="auth.user.username || auth.user.email"
          :home-path="'/stls'"
          :notification-count="notificationCount"
          :isAuthenticated="auth.isAuthenticated"
          icon="user"
          @logout="handleLogout"
        />
      </template>

      <!-- Logged in: Traffic Operator -->
      <template v-else-if="auth.user.role === 'TrafficOperator'">
        <RouterLink to="/stls" class="btn">Home</RouterLink>
        <RouterLink to="/stls/analytics" class="btn-outline">Analytics</RouterLink>

        <!-- Operator Dashboard Section -->
<!-- Operator Dashboard Section -->
<div class="dashboard-section">
  <button class="btn-dashboard" @click="toggleIntersectionMenu">
    Operator Dashboard
  </button>

  <div v-if="showMenu" class="intersection-grid">
    <div
      v-for="i in intersections"
      :key="i.path"
      class="intersection-item"
      @click="goToIntersection(i.path)"
    >
      <img :src="i.icon" alt="Intersection icon" class="intersection-icon" />
      <span>{{ i.label }}</span>
    </div>
  </div>
</div>


        <UserMenu
          :username="auth.user.username || auth.user.email"
          :home-path="'/stls'"
          :notification-count="notificationCount"
          :isAuthenticated="auth.isAuthenticated"
          icon="traffic-light"
          @logout="handleLogout"
        />
      </template>

      <!-- Logged in: Admin -->
      <template v-else-if="auth.user.role === 'Admin'">
        <RouterLink to="/stls" class="btn">Home</RouterLink>
        <RouterLink to="/stls/users" class="btn-outline">Users</RouterLink>
        <RouterLink to="/stls/audits" class="btn-outline">User Audits</RouterLink>
        <RouterLink to="/stls/analytics" class="btn-outline">Analytics</RouterLink>


        <!-- Admin Dashboard Section -->
        <div class="dashboard-section">
          <RouterLink to="/stls/admin" class="btn-outline">Admin Dashboard</RouterLink>
        </div>

        <UserMenu
          :username="auth.user.username || auth.user.email"
          :home-path="'/stls'"
          :notification-count="notificationCount"
          :isAuthenticated="auth.isAuthenticated"
          icon="admin"
          @logout="handleLogout"
        />
      </template>
    </div>
  </header>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useUserStore } from '../stores/userStore'
import { useAnalyticsStore } from '../stores/analyticsStore'
import { logoutApi } from '../services/userApi'
import UserMenu from './UserMenu.vue'
import '../assets/topbar.css'

const auth = useUserStore()
const router = useRouter()
const notificationCount = ref(0)
const analytics = useAnalyticsStore()
const showMenu = ref(false)

const intersections = [
  { label: "Agiou Spyridonos", path: "/stls/intersection/agiou-spyridonos", icon: "/1.png" },
  { label: "Anatoliki Pyli", path: "/stls/intersection/anatoliki-pyli", icon: "/2.png" },
  { label: "Dytiki Pyli", path: "/stls/intersection/dytiki-pyli", icon: "/3.png" },
  { label: "Ekklisia", path: "/stls/intersection/ekklisia", icon: "/4.png" },
  { label: "Kentriki Pyli", path: "/stls/intersection/kentriki-pyli", icon: "/5.png" },
]

function toggleIntersectionMenu() {
  showMenu.value = !showMenu.value
}

function goToIntersection(path) {
  showMenu.value = false
  router.push(path)
}

// Computed alerts from Pinia store
const alerts = computed(() => analytics.latestAlerts || [])

// Get the latest alert dynamically
const latestAlert = computed(() => {
  if (!alerts.value.length) return null
  return [...alerts.value].sort((a, b) => new Date(b.CreatedAt) - new Date(a.CreatedAt))[0]
})

// Fetch alerts on mount + auto-refresh every 60s
onMounted(() => {
  analytics.fetchAlerts({})
  setInterval(() => analytics.fetchAlerts({}), 60000)
})

// Logout handler
function handleLogout() {
  logoutApi()
    .catch(err => console.error('[Logout API] Failed:', err))
    .finally(() => {
      auth.logout()
      router.push('/')
    })
}


</script>

<style scoped>
.middle-alerts {
  position: absolute;
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  align-items: center;
  gap: 0.6rem;
  background: linear-gradient(90deg, #ff4d4f, #ff7875);
  color: #fff;
  padding: 0.4rem 1rem;
  border-radius: 1.5rem;
  font-weight: 600;
  font-size: 0.9rem;
  box-shadow: 0 0 8px rgba(0, 0, 0, 0.25);
  white-space: nowrap;
  overflow: hidden;
  animation: slideIn 0.5s ease, pulse 2s infinite;
}

.alert-badge {
  background: #fff;
  color: #ff4d4f;
  border-radius: 50%;
  padding: 0.2rem 0.5rem;
  font-size: 0.8rem;
  font-weight: bold;
}

.alert-text strong {
  text-decoration: underline;
}

@keyframes pulse {
  0% { opacity: 1; }
  50% { opacity: 0.7; }
  100% { opacity: 1; }
}

@keyframes slideIn {
  from { transform: translate(-50%, -10px); opacity: 0; }
  to { transform: translate(-50%, 0); opacity: 1; }
}

.intersection-grid {
  position: absolute;
  top: 110%; /* slightly lower to avoid overlap */
  right: 1rem;
  background: rgba(20, 20, 20, 0.95);
  border: 1px solid #333;
  border-radius: 12px;
  box-shadow: 0 6px 16px rgba(0, 0, 0, 0.5);
  padding: 1rem;
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: 0.8rem;
  z-index: 999;
  backdrop-filter: blur(6px);
  animation: fadeIn 0.25s ease;
}

.intersection-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 0.8rem;
  background: #000;
  border: 1px solid #222;
  border-radius: 10px;
  cursor: pointer;
  transition: all 0.2s ease;
  color: #f9fafb;
  text-align: center;
  font-size: 0.9rem;
}

.intersection-item:hover {
  background: linear-gradient(145deg, #1f2937, #111827);
  border-color: #3b82f6;
  transform: scale(1.05);
  color: #3b82f6;
}

.intersection-icon {
  width: 112px;
  height: 72px;
  margin-bottom: 0.4rem;
  filter: brightness(0.9) saturate(1.1);
  transition: transform 0.2s ease;
}

.intersection-item:hover .intersection-icon {
  transform: scale(1.1);
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(-8px); }
  to { opacity: 1; transform: translateY(0); }
}

</style>
