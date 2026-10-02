import { userApi } from "./httpClients"; // centralized client

// ===============================
// AUTHENTICATION
// ===============================

// LOGIN
export async function loginApi({ email, password }) {
  const { data } = await userApi.post("/api/users/login", {
    Email: email,
    Password: password,
  });

  // Persist token & expiry
  if (data?.Token) {
    localStorage.setItem("stls_token", data.Token);
    localStorage.setItem("stls_token_exp", data.ExpiresAt);
  }

  return {
    token: data.Token,
    expiresAt: data.ExpiresAt,
  };
}

// REGISTER
export async function registerApi({ email, username, password, confirmPassword }) {
  const { data } = await userApi.post("/api/users/register", {
    Email: email,
    Username: username,
    Password: password,
    ConfirmPassword: confirmPassword,
  });
  return data;
}

// LOGOUT
export async function logoutApi() {
  try {
    await userApi.post("/api/users/logout");
  } finally {
    // Always clear local storage
    localStorage.removeItem("stls_token");
    localStorage.removeItem("stls_token_exp");
  }
}

// ===============================
// PROFILE MANAGEMENT
// ===============================

// GET PROFILE
export async function getProfileApi() {
  const { data } = await userApi.get("/api/users/profile");
  return data;
}

// UPDATE PROFILE
export async function updateProfileApi(form) {
  const { data } = await userApi.put("/api/users/update", {
    Email: form.email,
    Username: form.username,
    Password: form.password || null,
    ConfirmPassword: form.confirmPassword || null,
  });
  return data;
}

// ===============================
// PASSWORD MANAGEMENT
// ===============================

export async function resetPasswordApi(email, newPassword, confirmPassword) {
  const { data } = await userApi.post("/api/users/reset-password", {
    Email: email,
    NewPassword: newPassword,
    ConfirmPassword: confirmPassword,
  });
  return data;
}

// ===============================
// SUBSCRIPTIONS
// ===============================

export async function subscribeApi({ intersection, metric }) {
  const { data } = await userApi.post("/api/users/subscriptions/subscribe", {
    Intersection: intersection,
    Metric: metric,
  });
  return data;
}

// ===============================
// HEALTH CHECKS
// ===============================

export async function checkHealth() {
  const { data } = await userApi.get("/health");
  return data;
}

export async function checkReady() {
  const { data } = await userApi.get("/ready");
  return data;
}
