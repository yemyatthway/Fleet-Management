<template>
  <div class="dashboard-content">
    <div>
      <h1 class="section-title">Dashboard Overview</h1>
      <p class="section-subtitle">Monitor your fleet operations in real-time</p>
    </div>

    <MetricCards />

    <div class="grid grid-two">
      <TripActivityChart />
      <VehicleStatusChart />
    </div>

    <div class="grid grid-two">
      <RecentTripsTable />
      <NotificationsPanel />
    </div>
  </div>
</template>

<script setup>
import MetricCards from './MetricCards.vue'
import TripActivityChart from './TripActivityChart.vue'
import VehicleStatusChart from './VehicleStatusChart.vue'
import RecentTripsTable from './RecentTripsTable.vue'
import NotificationsPanel from './NotificationsPanel.vue'
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

@media (max-width: 1100px) {
  .grid-two {
    grid-template-columns: 1fr;
  }
}
</style>
