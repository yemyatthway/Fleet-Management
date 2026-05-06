<template>
  <div class="card-surface chart-card">
    <div class="chart-header">
      <h2 class="chart-title">Trip Activity</h2>
      <p class="chart-subtitle">Weekly trip totals by status</p>
    </div>

    <div class="donut-wrap" ref="donutWrap">
      <svg :viewBox="`0 0 ${size} ${size}`" class="donut" role="img" aria-label="Trip activity totals">
        <circle
          class="donut-track"
          :cx="center"
          :cy="center"
          :r="radius"
        />
        <circle
          v-for="segment in segments"
          :key="segment.name"
          class="donut-segment"
          :cx="center"
          :cy="center"
          :r="radius"
          :stroke="segment.color"
          :stroke-dasharray="`${segment.dash} ${circumference - segment.dash}`"
          :stroke-dashoffset="segment.offset"
          @mouseenter="showTooltip($event, segment)"
          @mouseleave="hideTooltip"
        />
      </svg>
      <div class="donut-center">
        <p class="donut-total">{{ total }}</p>
        <span>Total Trips</span>
      </div>
      <div
        v-if="tooltip.visible"
        class="tooltip"
        :style="{ left: `${tooltip.x}px`, top: `${tooltip.y}px` }"
      >
        <div class="tooltip-title">{{ tooltip.label }}</div>
        <div class="tooltip-value">{{ tooltip.value }} trips • {{ tooltip.percent }}%</div>
      </div>
    </div>

    <div class="status-list">
      <div v-for="item in data" :key="item.name" class="status-item">
        <div class="status-left">
          <span class="status-dot" :style="{ background: item.color }"></span>
          <span class="status-label">{{ item.name }}</span>
        </div>
        <span class="status-value">{{ item.value }}</span>
      </div>
      <div v-if="!data.length" class="empty-state">No trip data yet</div>
    </div>
  </div>
</template>

<script setup>
import { computed, reactive, ref } from 'vue'

const props = defineProps({
  statuses: {
    type: Array,
    default: () => []
  }
})

const palette = ['#2563eb', '#10b981', '#ef4444', '#f59e0b', '#7c3aed']
const data = computed(() =>
  props.statuses.map((item, index) => ({
    name: item.name,
    value: item.value,
    color: palette[index % palette.length]
  }))
)

const total = computed(() => data.value.reduce((sum, item) => sum + item.value, 0))
const size = 220
const center = size / 2
const radius = 70
const circumference = 2 * Math.PI * radius

const segments = computed(() => {
  let runningOffset = 0
  return data.value.map((item) => {
    const dash = total.value ? (item.value / total.value) * circumference : 0
    const segment = { ...item, dash, offset: -runningOffset }
    runningOffset += dash
    return segment
  })
})

const donutWrap = ref(null)
const tooltip = reactive({
  visible: false,
  x: 0,
  y: 0,
  label: '',
  value: 0,
  percent: 0
})

const showTooltip = (event, segment) => {
  const rect = donutWrap.value?.getBoundingClientRect()
  if (!rect) return
  tooltip.visible = true
  tooltip.label = segment.name
  tooltip.value = segment.value
  tooltip.percent = total.value ? Math.round((segment.value / total.value) * 100) : 0
  tooltip.x = event.clientX - rect.left + 12
  tooltip.y = event.clientY - rect.top - 12
}

const hideTooltip = () => {
  tooltip.visible = false
}
</script>

<style scoped src="./dashboard_styles/ChartCard.css"></style>
