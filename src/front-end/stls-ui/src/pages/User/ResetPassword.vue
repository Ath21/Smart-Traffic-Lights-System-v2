<template>
  <div class="login-page">
    <div class="overlay"></div>

    <div class="login-card">
      <div class="card-content">
        <h1 class="title">UNIWA STLS</h1>

        <form class="form" @submit.prevent="submit">
          <!-- Email -->
          <input
            v-model="email"
            type="email"
            required
            placeholder="Email"
            class="input"
          />

          <!-- New Password -->
          <div class="password-wrapper">
            <input
              v-model="newPassword"
              :type="showPassword ? 'text' : 'password'"
              required
              placeholder="New Password"
              class="input password-input"
              @keydown="checkCapsLock"
              @keyup="checkCapsLock"
            />
            <span class="toggle-eye" @click="toggleShowPassword">
              <svg v-if="!showPassword" xmlns="http://www.w3.org/2000/svg" fill="none"
                   viewBox="0 0 24 24" stroke="currentColor" class="eye-icon">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 
                         0-8.268-2.943-9.542-7a9.97 9.97 0 
                         012.19-3.568m3.287-2.57A9.956 
                         9.956 0 0112 5c4.477 0 8.268 2.943 
                         9.542 7a9.97 9.97 0 
                         01-4.132 5.411M15 12a3 3 0 
                         11-6 0 3 3 0 016 0zM3 3l18 18" />
              </svg>
              <svg v-else xmlns="http://www.w3.org/2000/svg" fill="none"
                   viewBox="0 0 24 24" stroke="currentColor" class="eye-icon">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
                      d="M2.458 12C3.732 7.943 7.523 5 
                         12 5c4.478 0 8.268 2.943 
                         9.542 7-1.274 4.057-5.064 7-9.542 
                         7-4.477 0-8.268-2.943-9.542-7z" />
              </svg>
            </span>
          </div>

          <!-- Confirm Password -->
          <div class="password-wrapper">
            <input
              v-model="confirmPassword"
              :type="showPassword ? 'text' : 'password'"
              required
              placeholder="Confirm Password"
              class="input password-input"
              @keydown="checkCapsLock"
              @keyup="checkCapsLock"
            />
          </div>

          <transition name="fade">
            <p v-if="capsLockOn" class="caps-warning">⚠ Caps Lock is ON</p>
          </transition>

          <button type="submit" class="submit-btn" :disabled="loading">
            {{ loading ? "Resetting…" : "RESET PASSWORD" }}
          </button>
        </form>

        <p 
          v-if="message" 
          :class="['message', { error: isError, success: isSuccess }]">
          {{ message }}
        </p>

        <p class="footer-text">
          Remembered password?
          <RouterLink to="/login" class="register-link">Login</RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from "vue";
import { useUserStore } from "../../stores/userStore";
import "../../assets/reset-password.css";

const email = ref("");
const newPassword = ref("");
const confirmPassword = ref("");
const showPassword = ref(false);
const capsLockOn = ref(false);

const loading = ref(false);
const message = ref("");
const isError = ref(false);
const isSuccess = ref(false);

const auth = useUserStore();

function toggleShowPassword() {
  showPassword.value = !showPassword.value;
}

function checkCapsLock(event) {
  capsLockOn.value = event.getModifierState && event.getModifierState("CapsLock");
}

async function submit() {
  if (newPassword.value !== confirmPassword.value) {
    message.value = "❌ Passwords do not match!";
    isError.value = true;
    isSuccess.value = false;
    return;
  }

  loading.value = true;
  try {
    await auth.resetPassword(email.value, newPassword.value, confirmPassword.value);
    message.value = "✅ Password reset successful!";
    isSuccess.value = true;
    isError.value = false;
  } catch (err) {
    const details =
      auth.error || 
      err.response?.data?.details || 
      err.response?.data?.error || 
      err.response?.data?.message || 
      err.message;

    message.value = "❌ " + details;
    isError.value = true;
    isSuccess.value = false;
  } finally {
    loading.value = false;
  }
}
</script>
