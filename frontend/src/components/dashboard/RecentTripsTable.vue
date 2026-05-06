<template>
  <div class="recent-trips-card">
    <div class="table-header">
      <div>
        <h2 class="table-title">{{ title }}</h2>
        <p class="table-subtitle">{{ subtitle }}</p>
      </div>
    </div>
    <div class="table-wrap">
      <table class="dashboard-table">
        <thead>
          <tr>
            <th>{{ firstColumnLabel }}</th>
            <th>Vehicle</th>
            <th>Driver</th>
            <th>Route</th>
            <th>Status</th>
            <th>Details</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="trip in pagedTrips" :key="trip.id">
            <td :data-label="firstColumnLabel"><strong>{{ trip.tripNumber || trip.id }}</strong></td>
            <td class="text-muted" data-label="Vehicle">{{ trip.vehicle }}</td>
            <td data-label="Driver">{{ trip.driver }}</td>
            <td data-label="Route">
              <div class="route">
                <v-icon icon="mdi-map-marker" size="18" />
                <span class="text-muted">{{ trip.route }}</span>
              </div>
            </td>
            <td data-label="Status">
              <span class="badge" :class="statusClass(trip.status)">
                <v-icon :icon="statusIcon(trip.status)" size="14" />
                {{ formatStatus(trip.status) }}
              </span>
            </td>
            <td class="text-muted" data-label="Details">{{ trip.details || `${trip.duration} • ${trip.distance}` }}</td>
          </tr>
          <tr v-if="!trips.length" class="empty-row">
            <td colspan="6" class="empty-cell">{{ emptyText }}</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div class="table-footer">
      <button v-if="linkTo" class="link-button" type="button" @click="goToLink">{{ linkLabel }}</button>
      <div v-if="totalPages > 1" class="pager">
        <span class="pager-info text-muted">Page {{ safePage }} of {{ totalPages }}</span>
        <div class="pager-actions">
          <button
            class="pager-button"
            type="button"
            :disabled="safePage === 1"
            @click="goToPage(safePage - 1)"
          >
            Prev
          </button>
          <button
            class="pager-button"
            type="button"
            :disabled="safePage === totalPages"
            @click="goToPage(safePage + 1)"
          >
            Next
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'

const props = defineProps({
  trips: {
    type: Array,
    default: () => []
  },
  title: {
    type: String,
    default: 'Recent Trips'
  },
  subtitle: {
    type: String,
    default: 'Latest fleet activity'
  },
  firstColumnLabel: {
    type: String,
    default: 'Trip ID'
  },
  emptyText: {
    type: String,
    default: 'No recent trips found'
  },
  linkLabel: {
    type: String,
    default: 'View All Trips ->'
  },
  linkTo: {
    type: String,
    default: '/trips'
  }
})

const pageSize = 3
const currentPage = ref(1)
const router = useRouter()
const trips = computed(() => props.trips)
const totalPages = computed(() => Math.max(1, Math.ceil(trips.value.length / pageSize)))
const safePage = computed(() => Math.min(currentPage.value, totalPages.value))
const pagedTrips = computed(() => {
  const start = (safePage.value - 1) * pageSize
  return trips.value.slice(start, start + pageSize)
})

const goToPage = (page) => {
  currentPage.value = Math.min(Math.max(1, page), totalPages.value)
}

const goToLink = () => {
  if (props.linkTo) router.push(props.linkTo)
}

const statusClass = (status) => {
  const normalized = String(status || '').toLowerCase()
  if (normalized === 'completed') return 'success'
  if (normalized === 'ongoing' || normalized === 'in transit' || normalized === 'active') return 'info'
  return 'warning'
}

const statusIcon = (status) => {
  const normalized = String(status || '').toLowerCase()
  if (normalized === 'completed') return 'mdi-check-circle-outline'
  if (normalized === 'ongoing' || normalized === 'in transit' || normalized === 'active') return 'mdi-timer-outline'
  return 'mdi-alert-circle-outline'
}

const formatStatus = (status) => String(status || '').charAt(0).toUpperCase() + String(status || '').slice(1)
</script>

<style scoped>
.recent-trips-card {
  overflow: hidden;
  padding: 20px;
  border-radius: 16px;
  border: 1px solid var(--fleet-border);
  background: #fff;
  box-shadow: 0 6px 12px rgba(15, 23, 42, 0.04);
}

.table-header {
  margin-bottom: 16px;
}

.table-title {
  margin: 0;
  font-size: 18px;
  font-weight: 700;
}

.table-subtitle {
  margin: 4px 0 0;
  font-size: 13px;
  color: var(--fleet-muted);
}

.table-wrap {
  overflow-x: auto;
  max-width: 100%;
  min-width: 0;
  width: 100%;
  -webkit-overflow-scrolling: touch;
}

.dashboard-table {
  width: 100%;
  min-width: 100%;
  border-collapse: separate;
  border-spacing: 0;
}

.dashboard-table th,
.dashboard-table td {
  white-space: nowrap;
  text-align: left;
}

.dashboard-table thead th {
  background: #f8fafc;
  color: #475569;
  font-size: 13px;
  letter-spacing: 0.02em;
  text-transform: uppercase;
  font-weight: 700;
  padding: 14px 16px;
}

.dashboard-table tbody td {
  padding: 14px 16px;
  background: #fff;
}

.dashboard-table tbody tr {
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.06);
}

.dashboard-table tbody tr td {
  border-bottom: 10px solid transparent;
}

.dashboard-table tbody tr:last-child td {
  border-bottom: 0;
}

.dashboard-table tbody tr:nth-child(even) td {
  background: #f8fafc;
}

.dashboard-table thead th:first-child,
.dashboard-table tbody tr td:first-child {
  border-radius: 12px 0 0 12px;
}

.dashboard-table thead th:last-child,
.dashboard-table tbody tr td:last-child {
  border-radius: 0 12px 12px 0;
}

.route {
  white-space: normal;
  min-width: 220px;
}

.route {
  display: flex;
  align-items: center;
  gap: 6px;
}

.empty-cell {
  text-align: center;
  padding: 40px 16px !important;
  color: var(--fleet-muted);
  background: #fff !important;
  border-radius: 12px !important;
}

.empty-row {
  box-shadow: none !important;
}

.table-footer {
  padding-top: 12px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}

.link-button {
  border: none;
  background: transparent;
  color: var(--fleet-primary);
  font-weight: 600;
  cursor: pointer;
}

.link-button:hover {
  color: var(--fleet-primary-dark);
}

.pager {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.pager-actions {
  display: inline-flex;
  gap: 8px;
}

.pager-button {
  border: 1px solid var(--fleet-border);
  background: #fff;
  color: var(--fleet-text);
  font-size: 12px;
  font-weight: 600;
  padding: 6px 12px;
  border-radius: 10px;
  cursor: pointer;
}

.pager-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

@media (max-width: 900px) {
  .table-wrap {
    overflow-x: auto;
  }

  .dashboard-table {
    width: 100%;
    min-width: 760px;
  }

  .dashboard-table th,
  .dashboard-table td {
    padding: 10px 12px;
    font-size: 12px;
    white-space: nowrap;
  }

  .route {
    min-width: 0;
  }
}

@media (max-width: 720px) {
  .table-header {
    margin-bottom: 12px;
  }

  .table-footer {
    padding-top: 12px;
  }

  .table-wrap {
    overscroll-behavior-x: contain;
    scrollbar-gutter: stable both-edges;
    padding-bottom: 6px;
  }
}
</style>
