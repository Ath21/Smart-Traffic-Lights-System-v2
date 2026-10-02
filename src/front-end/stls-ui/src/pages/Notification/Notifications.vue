<template>
  <div class="subscriptions-page">
    <h2>🔔 Your Subscriptions</h2>

    <!-- Loading / Error States -->
    <div v-if="isLoading">Loading...</div>
    <div v-else-if="error">⚠️ {{ error }}</div>

    <!-- Subscriptions List -->
    <ul v-else-if="subscriptions.length > 0" class="subscriptions-list">
      <li v-for="sub in subscriptions" :key="sub.Intersection + sub.Metric" class="subscription-item">
        <strong>{{ sub.Intersection }}</strong> - {{ sub.Metric }}
        <button
          @click="unsubscribe(sub.Intersection, sub.Metric)"
          class="unsubscribe-btn"
          :disabled="isLoading"
        >
          Unsubscribe
        </button>
      </li>
    </ul>

    <!-- Empty State -->
    <p v-else>You have no subscriptions yet.</p>
  </div>
</template>

<script setup>
import { onMounted } from "vue";
import { storeToRefs } from "pinia";
import { useNotificationStore } from "../../stores/notificationStore";
import "../../assets/notifications.css";

const notificationStore = useNotificationStore();
const { subscriptions, isLoading, error } = storeToRefs(notificationStore);

// ===============================
// Unsubscribe action
// ===============================
async function unsubscribe(intersection, metric) {
  await notificationStore.removeSubscription(intersection, metric);
}

// ===============================
// Fetch subscriptions on mount
// ===============================
onMounted(() => notificationStore.fetchSubscriptions());
</script>
