<template>
  <div class="metric-grid">
    <div v-for="metric in metrics" :key="metric.title" class="metric-card">
      <div>
        <p class="metric-title">{{ metric.title }}</p>
        <p class="metric-value">{{ metric.value }}</p>
        <div class="metric-change">
          <v-icon :icon="metric.changeType === 'increase' ? 'mdi-trending-up' : 'mdi-trending-down'" size="18" :color="metric.changeType === 'increase' ? 'success' : 'error'" />
          <span :class="['change-number', metric.changeType]">{{ metric.change }}</span>
          <span class="change-label">this week</span>
        </div>
      </div>
      <div class="metric-icon" :style="{ background: metric.bgColor, color: metric.iconColor }">
        <v-icon :icon="metric.icon" size="24" />
      </div>
    </div>
  </div>
</template>

<script setup>
const metrics = [
  {
    title: 'Total Vehicles',
    value: '248',
    change: '+12',
    changeType: 'increase',
    icon: 'mdi-truck',
    bgColor: '#dbeafe',
    iconColor: '#2563eb'
  },
  {
    title: 'Active Trips',
    value: '67',
    change: '+5',
    changeType: 'increase',
    icon: 'mdi-map-marker',
    bgColor: '#dcfce7',
    iconColor: '#16a34a'
  },
  {
    title: 'Drivers On Duty',
    value: '142',
    change: '-8',
    changeType: 'decrease',
    icon: 'mdi-account-multiple',
    bgColor: '#ede9fe',
    iconColor: '#7c3aed'
  },
  {
    title: 'Under Maintenance',
    value: '23',
    change: '+3',
    changeType: 'increase',
    icon: 'mdi-wrench',
    bgColor: '#ffedd5',
    iconColor: '#ea580c'
  }
]
</script>

<style scoped>
.metric-grid {
  display: grid;
  gap: 20px;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
}

.metric-card {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding: 20px;
  border-radius: 16px;
  border: 1px solid var(--fleet-border);
  background: #fff;
  box-shadow: 0 6px 12px rgba(15, 23, 42, 0.04);
}

.metric-title {
  margin: 0;
  font-size: 13px;
  font-weight: 600;
  color: var(--fleet-muted);
}

.metric-value {
  font-size: 28px;
  font-weight: 800;
  margin: 8px 0 0;
}

.metric-change {
  display: flex;
  align-items: center;
  gap: 6px;
  margin-top: 8px;
}

.change-number {
  font-weight: 700;
  font-size: 13px;
}

.change-number.increase {
  color: var(--fleet-success);
}

.change-number.decrease {
  color: var(--fleet-danger);
}

.change-label {
  color: var(--fleet-muted);
  font-size: 12px;
}

.metric-icon {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  display: grid;
  place-items: center;
}
</style>
