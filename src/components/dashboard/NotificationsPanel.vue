<template>
  <div class="card-surface">
    <div class="panel-header">
      <div>
        <h2 class="panel-title">Notifications</h2>
        <p class="panel-subtitle">Recent activity updates</p>
      </div>
      <span class="count-pill">{{ notifications.length }}</span>
    </div>

    <div class="panel-list">
      <div v-for="item in notifications" :key="item.id" class="panel-item">
        <div class="panel-icon" :style="{ background: item.bg, color: item.color }">
          <v-icon :icon="item.icon" size="20" />
        </div>
        <div class="panel-content">
          <div class="panel-label">{{ item.title }}</div>
          <div class="panel-text">{{ item.message }}</div>
          <div class="panel-time">{{ item.time }}</div>
        </div>
      </div>
    </div>

    <div v-if="showViewAll" class="panel-footer">
      <button class="link-button" type="button" @click="handleViewAll">View All Notifications →</button>
    </div>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'

const emit = defineEmits(['view-all'])
defineProps({
  showViewAll: {
    type: Boolean,
    default: true
  }
})

const router = useRouter()

const handleViewAll = () => {
  router.push('/notifications')
  emit('view-all')
}

const notifications = [
  {
    id: 1,
    type: 'warning',
    title: 'Maintenance Due',
    message: 'Vehicle FL-2845 requires scheduled maintenance',
    time: '10 min ago',
    icon: 'mdi-alert-outline',
    bg: '#ffedd5',
    color: '#ea580c'
  },
  {
    id: 2,
    type: 'success',
    title: 'Trip Completed',
    message: 'John Martinez completed trip TRP-2456 successfully',
    time: '25 min ago',
    icon: 'mdi-check-circle-outline',
    bg: '#dcfce7',
    color: '#16a34a'
  },
  {
    id: 3,
    type: 'info',
    title: 'New Driver Added',
    message: 'Michael Roberts joined the driver pool',
    time: '1 hour ago',
    icon: 'mdi-information-outline',
    bg: '#dbeafe',
    color: '#2563eb'
  },
  {
    id: 4,
    type: 'alert',
    title: 'Fuel Alert',
    message: 'Vehicle FL-3091 fuel level below 20%',
    time: '2 hours ago',
    icon: 'mdi-alert-circle-outline',
    bg: '#fee2e2',
    color: '#dc2626'
  },
  {
    id: 5,
    type: 'success',
    title: 'Route Optimized',
    message: 'System optimized 5 routes, saving 45 miles',
    time: '3 hours ago',
    icon: 'mdi-check-circle-outline',
    bg: '#dcfce7',
    color: '#16a34a'
  }
]
</script>

<style scoped>
.panel-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border-bottom: 1px solid var(--fleet-border);
}

.panel-title {
  margin: 0;
  font-size: 18px;
  font-weight: 700;
}

.panel-subtitle {
  margin: 4px 0 0;
  font-size: 13px;
  color: var(--fleet-muted);
}

.count-pill {
  background: #dbeafe;
  color: #1d4ed8;
  font-weight: 700;
  font-size: 12px;
  padding: 4px 10px;
  border-radius: 999px;
}

.panel-list {
  display: flex;
  flex-direction: column;
}

.panel-item {
  display: flex;
  gap: 12px;
  padding: 14px 18px;
  border-bottom: 1px solid var(--fleet-border);
  cursor: pointer;
  transition: background 0.2s ease;
}

.panel-item:hover {
  background: #f8fafc;
}

.panel-icon {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  display: grid;
  place-items: center;
}

.panel-label {
  font-weight: 600;
}

.panel-text {
  font-size: 13px;
  color: var(--fleet-muted);
  margin-top: 2px;
}

.panel-time {
  font-size: 12px;
  color: #94a3b8;
  margin-top: 6px;
}

.panel-footer {
  padding: 12px 20px;
  text-align: center;
}

.link-button {
  border: none;
  background: transparent;
  color: var(--fleet-primary);
  font-weight: 600;
  cursor: pointer;
}

.link-button:hover {
  color: var(--fleet-primary-dark);
}

</style>
