<template>
  <div v-if="isAuthenticated" class="flex items-center gap-3 relative">
    <!-- Human Icon Button -->
    <button class="user-btn" @click="toggleMenu">
      <div class="icon-circle">
        <component :is="iconComponent" class="icon-svg" />
      </div>
      <NotificationBadge v-if="notificationCount" :count="notificationCount" />
    </button>

    <!-- Dropdown -->
    <div v-if="showMenu" class="dropdown">
      <div class="dropdown-header">{{ username }}</div>
      <RouterLink :to="homePath" class="dropdown-item">Home</RouterLink>
      <RouterLink to="/stls/profile" class="dropdown-item">Profile</RouterLink>
      
      <!-- Only show Subscriptions for regular users -->
      <RouterLink
        to="/stls/subscriptions"
        class="dropdown-item"
      >
        Subscriptions
        <NotificationBadge :count="notificationCount" inline />
      </RouterLink>

      <RouterLink to="/stls/update" class="dropdown-item">Update Profile</RouterLink>
      <button @click="$emit('logout')" class="dropdown-item logout">
        Logout
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { RouterLink } from 'vue-router'
import NotificationBadge from './NotificationBadge.vue'
import { UserIcon, Cog6ToothIcon, ShieldCheckIcon } from '@heroicons/vue/24/solid'
import '../assets/user-menu.css'

const props = defineProps({
  username: String,
  homePath: String,
  notificationCount: Number,
  isAuthenticated: Boolean,
  icon: String, // 'user', 'traffic-light', 'admin'
  role: String // added prop
})
const emit = defineEmits(['logout'])
const showMenu = ref(false)
const toggleMenu = () => (showMenu.value = !showMenu.value)

const iconComponent = computed(() => {
  switch (props.icon) {
    case 'traffic-light': return Cog6ToothIcon
    case 'admin': return ShieldCheckIcon
    default: return UserIcon
  }
})

// Close dropdown if clicked outside
function handleClickOutside(e) {
  const dropdown = document.querySelector('.dropdown')
  const btn = document.querySelector('.user-btn')
  if (dropdown && btn && !dropdown.contains(e.target) && !btn.contains(e.target)) {
    showMenu.value = false
  }
}
onMounted(() => document.addEventListener('click', handleClickOutside))
onBeforeUnmount(() => document.removeEventListener('click', handleClickOutside))
</script>
