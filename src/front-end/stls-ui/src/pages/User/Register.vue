<template>
  <div class="register-page">
    <div class="overlay"></div>

    <div class="register-card">
      <div class="card-content">
        <h1 class="title">UNIWA STLS</h1>

        <form class="form" @submit.prevent="submit">
          <input v-model="email" type="email" required placeholder="Email" class="input" />
          <input v-model="username" type="text" required placeholder="Username" class="input" />

          <!-- Password -->
          <div class="password-wrapper">
            <input
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              required
              placeholder="Password"
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
                         9.542 7-1.274 
                         4.057-5.064 7-9.542 
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

          <button :disabled="loading" class="submit-btn">
            {{ loading ? 'Registering…' : 'REGISTER' }}
          </button>
        </form>

        <p v-if="message" :class="['message', { error: isError, success: isSuccess }]">
          {{ message }}
        </p>

        <p class="footer-text">
          Have an account?
          <RouterLink to="/login" class="login-link">Log in</RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from "vue";
import { useUserStore } from "../../stores/userStore";
import { useRouter } from "vue-router";
import "../../assets/register.css";

const email = ref("");
const username = ref("");
const password = ref("");
const confirmPassword = ref("");

const loading = ref(false);
const message = ref("");
const isError = ref(false);
const isSuccess = ref(false);

const showPassword = ref(false);
const capsLockOn = ref(false);

const auth = useUserStore();
const router = useRouter();

function toggleShowPassword() {
  showPassword.value = !showPassword.value;
}

function checkCapsLock(event) {
  capsLockOn.value = event.getModifierState && event.getModifierState("CapsLock");
}

async function submit() {
  if (loading.value) return;

  if (password.value !== confirmPassword.value) {
    message.value = "❌ Passwords do not match!";
    isError.value = true;
    isSuccess.value = false;
    return;
  }

  loading.value = true;
  try {
    await auth.register({
      email: email.value,
      username: username.value,
      password: password.value,
      confirmPassword: confirmPassword.value,
    });
    message.value = "✅ Registration successful!";
    isSuccess.value = true;
    isError.value = false;

    // Navigate to login page
    router.push("/login");
  } catch (err) {
    const details = auth.error || err.response?.data?.details || err.response?.data?.error || err.message;
    message.value = "❌ " + details;
    isError.value = true;
    isSuccess.value = false;
  } finally {
    loading.value = false;
  }
}
</script>
