<template>
  <div class="card-surface chart-card">
    <div class="chart-header">
      <h2 class="chart-title">Vehicle Status</h2>
      <p class="chart-subtitle">Current fleet distribution</p>
    </div>

    <div class="donut-wrap" ref="donutWrap">
      <svg :viewBox="`0 0 ${size} ${size}`" class="donut">
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
        <span>Total Vehicles</span>
      </div>
      <div
        v-if="tooltip.visible"
        class="tooltip"
        :style="{ left: `${tooltip.x}px`, top: `${tooltip.y}px` }"
      >
        <div class="tooltip-title">{{ tooltip.label }}</div>
        <div class="tooltip-value">{{ tooltip.value }} vehicles • {{ tooltip.percent }}%</div>
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
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'

const data = [
  { name: 'Active', value: 165, color: '#10b981' },
  { name: 'Idle', value: 60, color: '#f59e0b' },
  { name: 'Maintenance', value: 23, color: '#ef4444' }
]

const total = data.reduce((sum, item) => sum + item.value, 0)
const size = 220
const center = size / 2
const radius = 70
const circumference = 2 * Math.PI * radius

let runningOffset = 0
const segments = data.map((item) => {
  const dash = (item.value / total) * circumference
  const segment = {
    ...item,
    dash,
    offset: -runningOffset
  }
  runningOffset += dash
  return segment
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
  tooltip.percent = Math.round((segment.value / total) * 100)
  tooltip.x = event.clientX - rect.left + 12
  tooltip.y = event.clientY - rect.top - 12
}

const hideTooltip = () => {
  tooltip.visible = false
}
</script>

<style scoped>
.chart-card {
  padding: 20px;
}

.chart-header {
  margin-bottom: 12px;
}

.chart-title {
  margin: 0;
  font-size: 18px;
  font-weight: 700;
}

.chart-subtitle {
  margin: 4px 0 0;
  font-size: 13px;
  color: var(--fleet-muted);
}

.donut-wrap {
  position: relative;
  display: grid;
  place-items: center;
  margin: 8px 0 16px;
}

.donut {
  width: 220px;
  height: 220px;
  transform: rotate(-90deg);
}

.donut-track {
  fill: none;
  stroke: #e2e8f0;
  stroke-width: 14;
}

.donut-segment {
  fill: none;
  stroke-width: 14;
  stroke-linecap: round;
  cursor: pointer;
}

.donut-center {
  position: absolute;
  text-align: center;
}

.donut-total {
  margin: 0;
  font-size: 26px;
  font-weight: 800;
}

.donut-center span {
  font-size: 12px;
  color: var(--fleet-muted);
}

.status-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.status-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 14px;
}

.status-left {
  display: flex;
  align-items: center;
  gap: 8px;
}

.status-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
}

.status-label {
  color: var(--fleet-muted);
}

.status-value {
  font-weight: 700;
}

.tooltip {
  position: absolute;
  background: #0f172a;
  color: #fff;
  padding: 8px 10px;
  border-radius: 10px;
  font-size: 12px;
  pointer-events: none;
  white-space: nowrap;
  box-shadow: 0 8px 16px rgba(15, 23, 42, 0.25);
}

.tooltip-title {
  font-weight: 700;
  margin-bottom: 2px;
}
</style>
