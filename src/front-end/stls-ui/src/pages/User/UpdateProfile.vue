<template>
  <div class="update-profile-page">
    <div class="update-card">
      <h2>Update Profile</h2>

      <form class="form" @submit.prevent="submit">
        <!-- Email -->
        <label>Email</label>
        <input v-model="form.email" type="email" required class="input" />

        <!-- Username -->
        <label>Username</label>
        <input v-model="form.username" type="text" required class="input" />

        <!-- Password -->
        <label>New Password</label>
        <input
          v-model="form.password"
          type="password"
          placeholder="Leave blank to keep old password"
          class="input"
        />

        <!-- Confirm Password -->
        <label>Confirm New Password</label>
        <input
          v-model="form.confirmPassword"
          type="password"
          placeholder="Re-enter new password"
          class="input"
        />

        <!-- Error / Success Message -->
        <p v-if="error" class="error-msg">❌ {{ error }}</p>
        <p v-if="success" class="success-msg">✅ Profile updated successfully!</p>

        <button class="btn" type="submit" :disabled="loading">
          {{ loading ? "Saving…" : "Save Changes" }}
        </button>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from "vue";
import { useUserStore } from "../../stores/userStore";
import "../../assets/update-profile.css";

const auth = useUserStore();

const form = reactive({
  email: auth.user?.email || "",
  username: auth.user?.username || "",
  password: "",
  confirmPassword: "",
});

const error = ref("");
const success = ref(false);
const loading = ref(false);

async function submit() {
  error.value = "";
  success.value = false;

  if (form.password && form.password !== form.confirmPassword) {
    error.value = "Passwords do not match!";
    return;
  }

  loading.value = true;
  try {
    await auth.updateProfile(form);

    // Update local Pinia state
    auth.user = {
      ...auth.user,
      email: form.email,
      username: form.username,
    };

    success.value = true;
    form.password = "";
    form.confirmPassword = "";
  } catch (err) {
    console.error("[UpdateProfile] error:", err);
    error.value = auth.error || err.response?.data?.message || "❌ Failed to update profile.";
  } finally {
    loading.value = false;
  }
}
</script>
