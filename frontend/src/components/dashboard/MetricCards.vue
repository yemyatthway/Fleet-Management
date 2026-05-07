<template>
  <div class="metric-grid">
    <div
      v-for="metric in normalizedMetrics"
      :key="metric.title"
      class="metric-card"
    >
      <div>
        <p class="metric-title">{{ metric.title }}</p>
        <p class="metric-value">{{ metric.value }}</p>
        <div class="metric-change">
          <v-icon
            :icon="
              metric.changeType === 'increase'
                ? 'mdi-trending-up'
                : 'mdi-trending-down'
            "
            size="18"
            :color="metric.changeType === 'increase' ? 'success' : 'error'"
          />
          <span :class="['change-number', metric.changeType]">{{
            metric.change
          }}</span>
          <span class="change-label">{{ metric.changeLabel }}</span>
        </div>
      </div>
      <div
        class="metric-icon"
        :style="{ background: metric.bgColor, color: metric.iconColor }"
      >
        <v-icon :icon="metric.icon" size="24" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from "vue";
const props = defineProps({
  metrics: {
    type: Array,
    default: () => [],
  },
});

const toneMap = {
  info: { bgColor: "#dbeafe", iconColor: "#2563eb" },
  success: { bgColor: "#dcfce7", iconColor: "#16a34a" },
  warning: { bgColor: "#ffedd5", iconColor: "#ea580c" },
  danger: { bgColor: "#fee2e2", iconColor: "#dc2626" },
  purple: { bgColor: "#ede9fe", iconColor: "#7c3aed" },
};

const normalizedMetrics = computed(() =>
  props.metrics.map((metric) => ({
    title: metric.title,
    value: String(metric.value ?? 0),
    change: "Live",
    changeType: "increase",
    changeLabel: "from backend",
    icon: metric.icon,
    ...(toneMap[metric.tone] || toneMap.info),
  })),
);
</script>

<style scoped src="./dashboard_styles/MetricCards.css"></style>
