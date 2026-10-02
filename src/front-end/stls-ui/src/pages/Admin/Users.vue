<template>
  <div class="users-page">
    <h2 class="page-title">User Management</h2>

    <div v-if="loading" class="loading">Loading users...</div>

    <table v-else-if="users.length" class="users-table">
      <thead>
        <tr>
          <th>ID</th>
          <th>Username</th>
          <th>Email</th>
          <th>Role</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="u in users" :key="u.UserId">
          <td>{{ u.UserId }}</td>
          <td>{{ u.Username }}</td>
          <td>{{ u.Email }}</td>
          <td>{{ u.Role }}</td>
          <td>
            <span :class="['status-badge', u.status === 'active' ? 'active' : 'inactive']">
              {{ u.status }}
            </span>
          </td>
          <td>
            <button
              class="btn-delete"
              @click="deleteUser(u.id)"
              :disabled="deletingIds.includes(u.id)"
            >
              {{ deletingIds.includes(u.id) ? "Deleting..." : "Delete" }}
            </button>
          </td>
        </tr>
      </tbody>
    </table>

    <div v-else class="no-users">No users found.</div>
    <div v-if="error" class="error">{{ error }}</div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from "vue";
import { useUserStore } from "../../stores/userStore";
import { userApi } from "../../services/httpClients";
import "../../assets/users.css";

const userStore = useUserStore();

const users = ref([]);
const loading = ref(false);
const error = ref(null);
const deletingIds = ref([]);

// ------------------------------
// Fetch users
// ------------------------------
async function fetchUsers() {
  loading.value = true;
  error.value = null;
  try {
    const res = await userApi.get("/api/users/all", {
      headers: { Authorization: `Bearer ${userStore.token}` },
    });
    users.value = res.data;
  } catch (err) {
    console.error("[UsersPage] fetchUsers error:", err);
    error.value = err.response?.data?.message || err.message || "Failed to load users";
  } finally {
    loading.value = false;
  }
}

// ------------------------------
// Delete user
// ------------------------------
async function deleteUser(userId) {
  if (!confirm("Are you sure you want to delete this user?")) return;

  deletingIds.value.push(userId);
  try {
    await userApi.delete(`/api/users/${userId}`, {
      headers: { Authorization: `Bearer ${userStore.token}` },
    });
    users.value = users.value.filter(u => u.id !== userId);
  } catch (err) {
    console.error("[UsersPage] deleteUser error:", err);
    error.value = err.response?.data?.message || err.message || "Failed to delete user";
  } finally {
    deletingIds.value = deletingIds.value.filter(id => id !== userId);
  }
}

// ------------------------------
// Lifecycle
// ------------------------------
onMounted(() => {
  fetchUsers();
});
</script>
