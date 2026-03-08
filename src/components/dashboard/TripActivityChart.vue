<template>
  <div class="card-surface chart-card">
    <div class="chart-header">
      <div>
        <h2 class="chart-title">Trip Activity</h2>
        <p class="chart-subtitle">Weekly trip statistics</p>
      </div>
      <div class="legend">
        <div v-for="line in series" :key="line.key" class="legend-item">
          <span class="legend-dot" :style="{ background: line.color }"></span>
          <span>{{ line.label }}</span>
        </div>
      </div>
    </div>

    <div class="chart-body" ref="chartBody">
      <svg :viewBox="`0 0 ${width} ${height}`" class="chart-svg" role="img">
        <g class="grid">
          <line
            v-for="y in gridLines"
            :key="y"
            :x1="padding"
            :x2="width - padding"
            :y1="y"
            :y2="y"
          />
        </g>

        <g class="axes">
          <line :x1="padding" :y1="height - padding" :x2="width - padding" :y2="height - padding" />
          <line :x1="padding" :y1="padding" :x2="padding" :y2="height - padding" />
        </g>

        <g class="labels">
          <text
            v-for="(label, index) in labels"
            :key="label"
            :x="xFor(index)"
            :y="height - 10"
            text-anchor="middle"
          >
            {{ label }}
          </text>
        </g>

        <g v-for="line in series" :key="line.key" class="series">
          <polyline
            :points="pointsFor(line.data)"
            :stroke="line.color"
          />
          <circle
            v-for="(value, idx) in line.data"
            :key="`${line.key}-${idx}`"
            :cx="xFor(idx)"
            :cy="yFor(value)"
            :fill="line.color"
            r="4"
            class="data-point"
            @mouseenter="showTooltip($event, line.label, value, labels[idx])"
            @mouseleave="hideTooltip"
          />
        </g>
      </svg>
      <div
        v-if="tooltip.visible"
        class="tooltip"
        :style="{ left: `${tooltip.x}px`, top: `${tooltip.y}px` }"
      >
        <div class="tooltip-title">{{ tooltip.label }}</div>
        <div class="tooltip-value">{{ tooltip.day }}: {{ tooltip.value }}</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'

const labels = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun']

const series = [
  { key: 'completed', label: 'Completed', color: '#2563eb', data: [45, 52, 48, 61, 55, 38, 42] },
  { key: 'ongoing', label: 'Ongoing', color: '#10b981', data: [12, 15, 18, 14, 20, 16, 11] },
  { key: 'cancelled', label: 'Cancelled', color: '#ef4444', data: [2, 1, 3, 2, 1, 2, 1] }
]

const width = 640
const height = 280
const padding = 36

const allValues = series.flatMap((s) => s.data)
const maxValue = Math.max(...allValues) + 5
const minValue = 0

const plotWidth = width - padding * 2
const plotHeight = height - padding * 2

const xFor = (index) => padding + (plotWidth / (labels.length - 1)) * index
const yFor = (value) => padding + ((maxValue - value) / (maxValue - minValue)) * plotHeight

const pointsFor = (data) => data.map((value, index) => `${xFor(index)},${yFor(value)}`).join(' ')

const gridLines = Array.from({ length: 5 }).map((_, idx) => padding + (plotHeight / 4) * idx)

const chartBody = ref(null)
const tooltip = reactive({
  visible: false,
  x: 0,
  y: 0,
  label: '',
  value: 0,
  day: ''
})

const showTooltip = (event, label, value, day) => {
  const rect = chartBody.value?.getBoundingClientRect()
  if (!rect) return
  tooltip.visible = true
  tooltip.label = label
  tooltip.value = value
  tooltip.day = day
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
  display: flex;
  flex-wrap: wrap;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}

.chart-title {
  margin: 0;
  font-size: 18px;
  font-weight: 700;
}

.chart-subtitle {
  margin: 4px 0 0;
  color: var(--fleet-muted);
  font-size: 13px;
}

.legend {
  display: flex;
  gap: 16px;
  align-items: center;
  font-size: 12px;
  color: var(--fleet-muted);
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 6px;
}

.legend-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
}

.chart-body {
  width: 100%;
  overflow-x: auto;
  position: relative;
}

.chart-svg {
  width: 100%;
  height: auto;
}

.grid line {
  stroke: #eef2f7;
  stroke-width: 1;
}

.axes line {
  stroke: #e2e8f0;
  stroke-width: 1;
}

.labels text {
  fill: #94a3b8;
  font-size: 12px;
}

.series polyline {
  fill: none;
  stroke-width: 2.5;
}

.data-point {
  cursor: pointer;
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
