<template>
  <div class="account-page">
    <h2>My Profile</h2>

    <div v-if="loading" class="loading">Loading...</div>
    <div v-else-if="error" class="error">Error: {{ error }}</div>

    <div v-else class="profile-card" v-if="user">
      <p><strong>Username:</strong> {{ user.username }}</p>
      <p><strong>Email:</strong> {{ user.email }}</p>
      <p><strong>Role:</strong> {{ user.role }}</p>
      <p><strong>Status:</strong> {{ user.status }}</p>
      <p><strong>Created at:</strong> {{ formatDate(user.createdAt) }}</p>
      <p><strong>Updated at:</strong> {{ formatDate(user.updatedAt) }}</p>
    </div>
  </div>
</template>

<script setup>
import { onMounted, computed } from "vue";
import { useUserStore } from "../../stores/userStore";
import { storeToRefs } from "pinia";
import "../../assets/profile.css";

// Pinia store
const auth = useUserStore();
const { user, loading, error } = storeToRefs(auth);

// Format date
function formatDate(dateStr) {
  if (!dateStr) return "—";
  return new Date(dateStr).toLocaleString();
}

// Fetch profile on mount
onMounted(async () => {
  try {
    const data = await auth.fetchProfile();

    // Adapt backend → frontend shape
    user.value = {
      id: data.UserId || data.id,
      username: data.Username || data.username,
      email: data.Email || data.email,
      role: data.Role || data.role,
      status: data.Status || data.status,
      createdAt: data.CreatedAt || data.createdAt,
      updatedAt: data.UpdatedAt || data.updatedAt,
    };
  } catch (err) {
    console.error("[Profile] fetch error:", err);
  }
});
</script>
