<template>
  <div class="metric-grid">
    <div v-for="metric in normalizedMetrics" :key="metric.title" class="metric-card">
      <div>
        <p class="metric-title">{{ metric.title }}</p>
        <p class="metric-value">{{ metric.value }}</p>
        <div class="metric-change">
          <v-icon :icon="metric.changeType === 'increase' ? 'mdi-trending-up' : 'mdi-trending-down'" size="18" :color="metric.changeType === 'increase' ? 'success' : 'error'" />
          <span :class="['change-number', metric.changeType]">{{ metric.change }}</span>
          <span class="change-label">{{ metric.changeLabel }}</span>
        </div>
      </div>
      <div class="metric-icon" :style="{ background: metric.bgColor, color: metric.iconColor }">
        <v-icon :icon="metric.icon" size="24" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
const props = defineProps({
  metrics: {
    type: Array,
    default: () => []
  }
})

const toneMap = {
  info: { bgColor: '#dbeafe', iconColor: '#2563eb' },
  success: { bgColor: '#dcfce7', iconColor: '#16a34a' },
  warning: { bgColor: '#ffedd5', iconColor: '#ea580c' },
  danger: { bgColor: '#fee2e2', iconColor: '#dc2626' },
  purple: { bgColor: '#ede9fe', iconColor: '#7c3aed' }
}

const normalizedMetrics = computed(() =>
  props.metrics.map((metric) => ({
    title: metric.title,
    value: String(metric.value ?? 0),
    change: 'Live',
    changeType: 'increase',
    changeLabel: 'from backend',
    icon: metric.icon,
    ...(toneMap[metric.tone] || toneMap.info)
  }))
)
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

@media (max-width: 900px) {
  .metric-grid {
    grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
    gap: 16px;
  }

  .metric-card {
    padding: 16px;
    border-radius: 14px;
  }

  .metric-value {
    font-size: 24px;
  }

  .metric-icon {
    width: 42px;
    height: 42px;
  }
}

@media (max-width: 560px) {
  .metric-grid {
    grid-template-columns: 1fr;
  }
}
</style>
