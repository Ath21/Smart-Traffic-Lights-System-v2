import { defineStore } from "pinia";
import { ref } from "vue";
import { notificationApi } from "../services/httpClients";
import { decodeToken } from "../utils/jwtHelper";

export const useNotificationStore = defineStore("notificationStore", () => {
  // ===============================
  // State
  // ===============================
  const subscriptions = ref([]);
  const notifications = ref([]);
  const unreadCount = ref(0);
  const isLoading = ref(false);
  const error = ref(null);

  // ===============================
  // Helpers to get current user info
  // ===============================
  function getUserId() {
    const token = localStorage.getItem("stls_token");
    const decoded = decodeToken(token);
    return decoded?.id || null;
  }

  function getUserEmail() {
    const token = localStorage.getItem("stls_token");
    const decoded = decodeToken(token);
    return decoded?.email || null;
  }

  // ===============================
  // Fetch user subscriptions
  // ===============================
  async function fetchSubscriptions() {
    const userId = getUserId();
    if (!userId) {
      error.value = "User not authenticated";
      return;
    }

    isLoading.value = true;
    error.value = null;

    try {
      const { data } = await notificationApi.get(`/api/notifications/subscriptions/${userId}`);
      subscriptions.value = data || [];
    } catch (err) {
      error.value = err.response?.data?.message || err.message || "Failed to fetch subscriptions";
      console.error("[NotificationStore] fetchSubscriptions error:", err);
    } finally {
      isLoading.value = false;
    }
  }

  // ===============================
  // Fetch notifications for the user
  // ===============================
  async function fetchNotifications(unreadOnly = false) {
    const userId = getUserId();
    if (!userId) {
      error.value = "User not authenticated";
      return;
    }

    isLoading.value = true;
    error.value = null;

    try {
      const { data } = await notificationApi.get(`/api/notifications/deliveries/${userId}`, {
        params: { unreadOnly },
      });
      notifications.value = data || [];
      unreadCount.value = notifications.value.filter(n => !n.Read).length;
    } catch (err) {
      error.value = err.response?.data?.message || err.message || "Failed to fetch notifications";
      console.error("[NotificationStore] fetchNotifications error:", err);
      notifications.value = [];
      unreadCount.value = 0;
    } finally {
      isLoading.value = false;
    }
  }

  // ===============================
  // Mark a single delivery as read
  // ===============================
  async function markAsRead(deliveryId) {
    const userId = getUserId();
    const email = getUserEmail();
    if (!userId || !email) {
      error.value = "User not authenticated";
      return;
    }

    try {
      await notificationApi.patch("/api/notifications/deliveries/read", {
        UserId: userId,
        UserEmail: email,
        DeliveryId: deliveryId,
      });

      // Update local state
      const notif = notifications.value.find(n => n.Id === deliveryId);
      if (notif) notif.Read = true;
      unreadCount.value = notifications.value.filter(n => !n.Read).length;
    } catch (err) {
      error.value = err.response?.data?.message || err.message || "Failed to mark notification as read";
      console.error("[NotificationStore] markAsRead error:", err);
    }
  }

  // ===============================
  // Unsubscribe from a subscription
  // ===============================
  async function removeSubscription(intersection, metric) {
    const userId = getUserId();
    const email = getUserEmail();
    if (!userId || !email) {
      error.value = "User not authenticated";
      return;
    }

    try {
      await notificationApi.delete("/api/notifications/subscriptions/unsubscribe", {
        params: {
          UserId: userId,
          UserEmail: email,
          Intersection: intersection,
          Metric: metric,
        },
      });

      subscriptions.value = subscriptions.value.filter(
        s => !(s.Intersection === intersection && s.Metric === metric)
      );
    } catch (err) {
      error.value = err.response?.data?.message || err.message || "Failed to unsubscribe";
      console.error("[NotificationStore] removeSubscription error:", err);
    }
  }

  // ===============================
  // Return state & actions
  // ===============================
  return {
    subscriptions,
    notifications,
    unreadCount,
    isLoading,
    error,
    fetchSubscriptions,
    fetchNotifications,
    markAsRead,
    removeSubscription,
  };
});
