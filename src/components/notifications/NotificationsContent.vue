<template>
  <div class="notifications-content">
    <div>
      <h1 class="section-title">Notifications</h1>
      <p class="section-subtitle">All recent activity across your fleet</p>
    </div>

    <NotificationsPanel :show-view-all="false" />
  </div>
</template>

<script setup>
import NotificationsPanel from '../dashboard/NotificationsPanel.vue'
</script>

<style scoped>
.notifications-content {
  display: flex;
  flex-direction: column;
  gap: 24px;
}
</style>
