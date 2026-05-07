<template>
  <div class="card-surface chart-card">
    <div class="chart-header">
      <h2 class="chart-title">{{ title }}</h2>
      <p class="chart-subtitle">{{ subtitle }}</p>
    </div>

    <div class="graph-summary">
      <div>
        <p class="graph-total">{{ total }}</p>
        <span>{{ totalLabel }}</span>
      </div>
      <span class="graph-unit">{{ unitLabel }}</span>
    </div>

    <div class="bar-chart" role="img" aria-label="Trip activity status graph">
      <div v-for="item in data" :key="item.name" class="bar-row">
        <div class="bar-label">
          <span class="status-dot" :style="{ background: item.color }" />
          <span>{{ item.name }}</span>
        </div>
        <div class="bar-track">
          <div class="bar-fill" :style="{ width: `${item.percent}%`, background: item.color }"></div>
        </div>
        <div class="bar-value">{{ item.value }}</div>
      </div>
      <div v-if="!data.length" class="empty-state">No trip data yet</div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  statuses: {
    type: Array,
    default: () => []
  },
  title: {
    type: String,
    default: 'Trip Activity'
  },
  subtitle: {
    type: String,
    default: 'Trip totals by status'
  },
  totalLabel: {
    type: String,
    default: 'Total Trips'
  },
  unitLabel: {
    type: String,
    default: 'trips'
  }
})

const palette = ['#2563eb', '#10b981', '#ef4444', '#f59e0b', '#7c3aed']
const total = computed(() => props.statuses.reduce((sum, item) => sum + Number(item.value || 0), 0))
const data = computed(() =>
  props.statuses.map((item, index) => ({
    name: item.name,
    value: Number(item.value || 0),
    percent: total.value ? Math.round((Number(item.value || 0) / total.value) * 100) : 0,
    color: palette[index % palette.length]
  }))
)
</script>

<style scoped src="./dashboard_styles/ChartCard.css"></style>
