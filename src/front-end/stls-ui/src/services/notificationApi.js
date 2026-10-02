import { notificationApi } from "./httpClients"; // centralized client

// ===== API FUNCTIONS =====

// Fetch user notifications
export async function getNotifications(userId, unreadOnly = false) {
  const { data } = await notificationApi.get(`/api/notifications/deliveries/${userId}`, {
    params: { unreadOnly }
  });
  return data;
}

// Mark a single delivery as read
export async function markAsRead(userId, email, deliveryId) {
  await notificationApi.patch("/api/notifications/deliveries/read", {
    UserId: userId,
    UserEmail: email,
    DeliveryId: deliveryId
  });
}

// Get subscriptions for a user
export async function getSubscriptions(userId) {
  const { data } = await notificationApi.get(`/api/notifications/subscriptions/${userId}`);
  return data;
}

// Unsubscribe from a subscription
export async function unsubscribe(userId, email, intersection, metric) {
  await notificationApi.delete("/api/notifications/subscriptions/unsubscribe", {
    params: {
      UserId: userId,
      UserEmail: email,
      Intersection: intersection,
      Metric: metric
    }
  });
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