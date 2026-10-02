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

          <!-- Caps Lock warning -->
          <transition name="fade">
            <p v-if="capsLockOn" class="caps-warning">⚠ Caps Lock is ON</p>
          </transition>

          <!-- Forgot password -->
          <router-link to="/reset-password" class="forgot-link">Forgot password?</router-link>

          <button :disabled="loading" class="submit-btn">
            {{ loading ? 'Logging in…' : 'LOGIN' }}
          </button>
        </form>

        <!-- Feedback -->
        <p v-if="error" class="message error">❌ {{ error }}</p>
        <p v-if="success" class="message success">✅ {{ success }}</p>

        <!-- Footer -->
        <p class="footer-text">
          Don’t have an account?
          <RouterLink to="/register" class="register-link">Register</RouterLink>
        </p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, nextTick, onMounted } from 'vue'
import { useUserStore } from '../../stores/userStore'
import { storeToRefs } from 'pinia'
import { useRouter } from 'vue-router'
import '../../assets/login.css'

const email = ref('')
const password = ref('')
const showPassword = ref(false)
const capsLockOn = ref(false)
const success = ref('')

// ========== Pinia store ==========
const auth = useUserStore()
const { loading, error, user } = storeToRefs(auth)

const router = useRouter()

// Redirect if already logged in
onMounted(() => {
  if (auth.isAuthenticated) router.replace('/stls')
})

// Clear success/error when user types
watch([email, password], () => {
  success.value = ''
})

// Toggle password visibility
function toggleShowPassword() {
  showPassword.value = !showPassword.value
}

// Detect Caps Lock
function checkCapsLock(event) {
  capsLockOn.value = event.getModifierState && event.getModifierState('CapsLock')
}

// Submit login
async function submit() {
  if (!email.value || !password.value || loading.value) return
  success.value = ''

  try {
    await auth.login(email.value, password.value)
    success.value = 'Login successful!'
    // Wait for reactivity to update
    await nextTick()
    router.push('/stls')
  } catch (err) {
    console.error(err)
  }
}
</script>
