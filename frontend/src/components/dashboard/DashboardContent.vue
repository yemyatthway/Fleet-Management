<template>
  <div class="dashboard-content">
    <div>
      <h1 class="section-title">{{ dashboardCopy.title }}</h1>
      <p class="section-subtitle">{{ dashboardCopy.subtitle }}</p>
    </div>

    <div v-if="error" class="dashboard-error">{{ error }}</div>

    <MetricCards :metrics="summary?.metrics || []" />

    <div class="grid graph-stack">
      <TripActivityChart
        :statuses="summary?.tripStatuses || []"
        :title="dashboardCopy.primaryChartTitle"
        :subtitle="dashboardCopy.primaryChartSubtitle"
        :total-label="dashboardCopy.primaryChartTotal"
        :unit-label="dashboardCopy.primaryChartUnit"
      />
      <VehicleStatusChart
        :statuses="summary?.vehicleStatuses || []"
        :title="dashboardCopy.secondaryChartTitle"
        :subtitle="dashboardCopy.secondaryChartSubtitle"
        :total-label="dashboardCopy.secondaryChartTotal"
        :unit-label="dashboardCopy.secondaryChartUnit"
      />
    </div>

    <div class="grid">
      <RecentTripsTable
        :trips="summary?.recentTrips || []"
        :title="dashboardCopy.tableTitle"
        :subtitle="dashboardCopy.tableSubtitle"
        :empty-text="dashboardCopy.tableEmpty"
        :link-label="dashboardCopy.tableLinkLabel"
        :link-to="dashboardCopy.tableLinkTo"
      />
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import MetricCards from './MetricCards.vue'
import TripActivityChart from './TripActivityChart.vue'
import VehicleStatusChart from './VehicleStatusChart.vue'
import RecentTripsTable from './RecentTripsTable.vue'
import { getDashboardSummary } from '../../services/dashboardApi'
import { getCurrentUser } from '../../utils/authSession'

const summary = ref(null)
const error = ref('')
const currentUser = computed(() => getCurrentUser())
const roleId = computed(() => String(currentUser.value?.roleId || currentUser.value?.role || '').toLowerCase())

const dashboardCopies = {
  admin: {
    title: 'Admin Dashboard',
    subtitle: 'System-wide fleet, users, compliance, and operations overview.',
    primaryChartTitle: 'Trip Activity',
    primaryChartSubtitle: 'All trip totals by status',
    primaryChartTotal: 'Total Trips',
    primaryChartUnit: 'trips',
    secondaryChartTitle: 'Vehicle Status',
    secondaryChartSubtitle: 'Current fleet distribution',
    secondaryChartTotal: 'Total Vehicles',
    secondaryChartUnit: 'vehicles',
    tableTitle: 'Recent Trips',
    tableSubtitle: 'Latest fleet activity',
    tableEmpty: 'No recent trips found',
    tableLinkLabel: 'View All Trips ->',
    tableLinkTo: '/trips'
  },
  dispatcher: {
    title: 'Dispatcher Dashboard',
    subtitle: 'Plan dispatches, watch active trips, and keep route exceptions visible.',
    primaryChartTitle: 'Dispatch Activity',
    primaryChartSubtitle: 'Trip workload by status',
    primaryChartTotal: 'Trips',
    primaryChartUnit: 'trips',
    secondaryChartTitle: 'Fleet Availability',
    secondaryChartSubtitle: 'Vehicles grouped by operating status',
    secondaryChartTotal: 'Vehicles',
    secondaryChartUnit: 'vehicles',
    tableTitle: 'Dispatch Queue',
    tableSubtitle: 'Trips needing operational attention',
    tableEmpty: 'No dispatch trips found',
    tableLinkLabel: 'Open Trips ->',
    tableLinkTo: '/trips'
  },
  driver: {
    title: 'Driver Dashboard',
    subtitle: 'Your assigned trips, vehicle status, and upcoming work.',
    primaryChartTitle: 'My Trips',
    primaryChartSubtitle: 'Your trips by status',
    primaryChartTotal: 'My Trips',
    primaryChartUnit: 'trips',
    secondaryChartTitle: 'My Vehicles',
    secondaryChartSubtitle: 'Vehicles assigned to you',
    secondaryChartTotal: 'Vehicles',
    secondaryChartUnit: 'vehicles',
    tableTitle: 'My Recent Trips',
    tableSubtitle: 'Your latest route activity',
    tableEmpty: 'No trips assigned to you yet',
    tableLinkLabel: 'Open My Trips ->',
    tableLinkTo: '/trips'
  },
  mechanic: {
    title: 'Mechanic Dashboard',
    subtitle: 'Assigned maintenance work, open incidents, and parts readiness.',
    primaryChartTitle: 'Ticket Workload',
    primaryChartSubtitle: 'Assigned tickets by status',
    primaryChartTotal: 'Tickets',
    primaryChartUnit: 'tickets',
    secondaryChartTitle: 'Fleet Maintenance Status',
    secondaryChartSubtitle: 'Vehicles by maintenance-related status',
    secondaryChartTotal: 'Vehicles',
    secondaryChartUnit: 'vehicles',
    tableTitle: 'Recent Maintenance Trips',
    tableSubtitle: 'Vehicles recently involved in fleet movement',
    tableEmpty: 'No recent fleet activity found',
    tableLinkLabel: 'Open Maintenance ->',
    tableLinkTo: '/maintenance'
  }
}

const dashboardCopy = computed(() => dashboardCopies[roleId.value] || dashboardCopies.admin)

const loadSummary = async () => {
  try {
    summary.value = await getDashboardSummary()
  } catch (loadError) {
    error.value = loadError.message || 'Could not load dashboard summary.'
  }
}

onMounted(loadSummary)
</script>

<style scoped src="./dashboard_styles/DashboardContent.css"></style>
