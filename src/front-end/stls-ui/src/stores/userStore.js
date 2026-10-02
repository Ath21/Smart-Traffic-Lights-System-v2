import { defineStore } from "pinia";
import { ref, computed } from "vue";
import { userApi } from "../services/httpClients";

// ===============================
// JWT Decoder
// ===============================
function decodeJWT(token) {
  try {
    const base64 = token.split(".")[1];
    if (!base64) return null;
    const padded = base64.replace(/-/g, "+").replace(/_/g, "/");
    const payload = JSON.parse(
      decodeURIComponent(
        atob(padded)
          .split("")
          .map((c) => "%" + ("00" + c.charCodeAt(0).toString(16)).slice(-2))
          .join("")
      )
    );
    return payload;
  } catch (err) {
    console.warn("[Auth] Failed to decode JWT:", err);
    return null;
  }
}

// ===============================
// User Store
// ===============================
export const useUserStore = defineStore("userStore", () => {
  // ===============================
  // State
  // ===============================
  const token = ref(localStorage.getItem("stls_token") || null);
  const user = ref(null);
  const loading = ref(false);
  const ready = ref(false);
  const error = ref(null);

  // ===============================
  // Derived
  // ===============================
  const isAuthenticated = computed(() => !!token.value && !!user.value);

  // ===============================
  // Bootstrap from localStorage
  // ===============================
  function bootstrap() {
    const saved = localStorage.getItem("stls_token");
    if (!saved) {
      ready.value = true;
      return;
    }

    const decoded = decodeJWT(saved);
    if (!decoded) {
      localStorage.removeItem("stls_token");
      ready.value = true;
      return;
    }

    user.value = {
      id: decoded.sub,
      email:
        decoded.email ||
        decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"] ||
        null,
      username:
        decoded["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] ||
        decoded.username ||
        decoded.email,
      role:
        decoded["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ||
        decoded.role ||
        "User",
      status: decoded.status || "active",
      exp: decoded.exp,
    };
    token.value = saved;
    ready.value = true;
  }

  // ===============================
  // Auth Actions
  // ===============================
  async function login(email, password) {
    loading.value = true;
    error.value = null;
    try {
      const { data } = await userApi.post("/api/users/login", { Email: email, Password: password });
      if (data?.Token) {
        token.value = data.Token;
        localStorage.setItem("stls_token", data.Token);
        localStorage.setItem("stls_token_exp", data.ExpiresAt);
        bootstrap(); // decode token and set user
      }
      return data;
    } catch (err) {
      error.value = err.response?.data?.message || err.message || "Login failed";
      console.error("[UserStore] login error:", err);
      
      throw err;
    } finally {
      loading.value = false;
    }
  }

  async function logout() {
    try {
      await userApi.post("/api/users/logout");
    } catch {
      /* ignore errors */
    } finally {
      token.value = null;
      user.value = null;
      localStorage.removeItem("stls_token");
      localStorage.removeItem("stls_token_exp");
    }
  }

  async function register({ email, username, password, confirmPassword }) {
    loading.value = true;
    error.value = null;
    try {
      const { data } = await userApi.post("/api/users/register", {
        Email: email,
        Username: username,
        Password: password,
        ConfirmPassword: confirmPassword,
      });
      return data;
    } catch (err) {
      error.value = err.response?.data?.message || err.message || "Registration failed";
      console.error("[UserStore] register error:", err);
      throw err;
    } finally {
      loading.value = false;
    }
  }

  // ===============================
  // Profile
  // ===============================
async function fetchProfile() {
  loading.value = true;
  error.value = null;
  try {
    const { data } = await userApi.get("/api/users/profile");
    
    // Map backend -> frontend
    user.value = {
      id: data.UserId || data.id,
      username: data.Username || data.username,
      email: data.Email || data.email,
      role: data.Role || data.role,
      status: data.Status || data.status,
      createdAt: data.CreatedAt || data.createdAt,
      updatedAt: data.UpdatedAt || data.updatedAt,
    };

    return user.value;
  } catch (err) {
    error.value = err.response?.data?.message || err.message || "Failed to fetch profile";
    console.error("[UserStore] fetchProfile error:", err);
    throw err;
  } finally {
    loading.value = false;
  }
}


  async function updateProfile(form) {
    loading.value = true;
    error.value = null;
    try {
      const payload = {
        Email: form.email,
        Username: form.username,
        Password: form.password || null,
        ConfirmPassword: form.confirmPassword || null,
      };
      const { data } = await userApi.put("/api/users/update", payload, {
        headers: { Authorization: `Bearer ${token.value}` },
      });
      return data;
    } catch (err) {
      error.value = err.response?.data?.message || err.message || "Failed to update profile";
      console.error("[UserStore] updateProfile error:", err);
      throw err;
    } finally {
      loading.value = false;
    }
  }

  // ===============================
  // Password reset
  // ===============================
  async function resetPassword(email, newPassword, confirmPassword) {
    loading.value = true;
    error.value = null;
    try {
      const { data } = await userApi.post("/api/users/reset-password", {
        Email: email,
        NewPassword: newPassword,
        ConfirmPassword: confirmPassword,
      });
      return data;
    } catch (err) {
      error.value = err.response?.data?.message || err.message || "Failed to reset password";
      console.error("[UserStore] resetPassword error:", err);
      throw err;
    } finally {
      loading.value = false;
    }
  }

  // ===============================
  // Subscriptions
  // ===============================
  async function subscribe(intersection, metric) {
    loading.value = true;
    error.value = null;
    try {
      const { data } = await userApi.post("/api/users/subscriptions/subscribe", {
        Intersection: intersection,
        Metric: metric,
      });
      return data;
    } catch (err) {
      error.value = err.response?.data?.message || err.message || "Failed to subscribe";
      console.error("[UserStore] subscribe error:", err);
      throw err;
    } finally {
      loading.value = false;
    }
  }

  // ===============================
  // Health checks
  // ===============================
  async function checkHealth() {
    try {
      const { data } = await userApi.get("/health");
      return data;
    } catch (err) {
      console.error("[UserStore] health error:", err);
      throw err;
    }
  }

  async function checkReady() {
    try {
      const { data } = await userApi.get("/ready");
      return data;
    } catch (err) {
      console.error("[UserStore] ready error:", err);
      throw err;
    }
  }

  // ===============================
  // Init
  // ===============================
  bootstrap();

  return {
    token,
    user,
    loading,
    ready,
    error,
    isAuthenticated,
    login,
    logout,
    register,
    fetchProfile,
    updateProfile,
    resetPassword,
    subscribe,
    checkHealth,
    checkReady,
    bootstrap,
  };
});
