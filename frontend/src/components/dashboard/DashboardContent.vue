<template>
  <div class="dashboard-content">
    <div>
      <h1 class="section-title">Dashboard Overview</h1>
      <p class="section-subtitle">Monitor your fleet operations in real-time</p>
    </div>

    <div v-if="error" class="dashboard-error">{{ error }}</div>

    <MetricCards :metrics="summary?.metrics || []" />

    <div class="grid grid-two">
      <TripActivityChart :statuses="summary?.tripStatuses || []" />
      <VehicleStatusChart :statuses="summary?.vehicleStatuses || []" />
    </div>

    <div class="grid">
      <RecentTripsTable :trips="summary?.recentTrips || []" />
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import MetricCards from './MetricCards.vue'
import TripActivityChart from './TripActivityChart.vue'
import VehicleStatusChart from './VehicleStatusChart.vue'
import RecentTripsTable from './RecentTripsTable.vue'
import { getDashboardSummary } from '../../services/dashboardApi'

const summary = ref(null)
const error = ref('')

const loadSummary = async () => {
  try {
    summary.value = await getDashboardSummary()
  } catch (loadError) {
    error.value = loadError.message || 'Could not load dashboard summary.'
  }
}

onMounted(loadSummary)
</script>

<style scoped>
.dashboard-content {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.grid {
  display: grid;
  gap: 24px;
}

.grid-two {
  grid-template-columns: minmax(0, 1.7fr) minmax(0, 1fr);
}

.dashboard-error {
  padding: 12px 14px;
  border: 1px solid #fecaca;
  border-radius: 12px;
  background: #fef2f2;
  color: #b91c1c;
}

@media (max-width: 1100px) {
  .grid-two {
    grid-template-columns: 1fr;
  }
}

@media (max-width: 900px) {
  .dashboard-content {
    gap: 18px;
  }

  .grid {
    gap: 18px;
  }
}
</style>
