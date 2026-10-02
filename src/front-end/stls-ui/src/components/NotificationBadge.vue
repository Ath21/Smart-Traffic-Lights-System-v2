<template>
  <!-- Circle badge (absolute, e.g. on icons) -->
  <span
    v-if="count > 0 && !inline"
    class="badge"
  >
    {{ displayValue }}
  </span>

  <!-- Inline badge (inside links / dropdowns) -->
  <span
    v-else-if="count > 0 && inline"
    class="inline-badge"
  >
    {{ displayValue }}
  </span>
</template>

<script setup>
import { computed } from 'vue'
import '../assets/notification-badge.css'

const props = defineProps({
  count: { type: Number, default: 0 },
  inline: { type: Boolean, default: false },
  max: { type: Number, default: 99 }
})

const displayValue = computed(() =>
  props.count > props.max ? `${props.max}+` : props.count
)
</script>

