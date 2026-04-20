<template>
  <div class="card-surface">
    <div class="table-header">
      <div>
        <h2 class="table-title">Recent Trips</h2>
        <p class="table-subtitle">Latest fleet activity</p>
      </div>
    </div>
    <div class="table-wrap">
      <table class="table-base">
        <thead>
          <tr>
            <th>Trip ID</th>
            <th>Vehicle</th>
            <th>Driver</th>
            <th>Route</th>
            <th>Status</th>
            <th>Details</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="trip in pagedTrips" :key="trip.id">
            <td data-label="Trip ID"><strong>{{ trip.id }}</strong></td>
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
            <td class="text-muted" data-label="Details">{{ trip.duration }} • {{ trip.distance }}</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div class="table-footer">
      <button class="link-button" type="button">View All Trips →</button>
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

const trips = [
  {
    id: 'TRP-2456',
    vehicle: 'FL-2845',
    driver: 'John Martinez',
    route: 'New York → Boston',
    status: 'completed',
    duration: '4h 25m',
    distance: '215 mi'
  },
  {
    id: 'TRP-2457',
    vehicle: 'FL-3091',
    driver: 'Sarah Johnson',
    route: 'Chicago → Detroit',
    status: 'ongoing',
    duration: '2h 15m',
    distance: '125 mi'
  },
  {
    id: 'TRP-2458',
    vehicle: 'FL-1724',
    driver: 'Mike Chen',
    route: 'Los Angeles → San Diego',
    status: 'completed',
    duration: '2h 30m',
    distance: '120 mi'
  },
  {
    id: 'TRP-2459',
    vehicle: 'FL-4532',
    driver: 'Emily Davis',
    route: 'Houston → Austin',
    status: 'ongoing',
    duration: '1h 45m',
    distance: '95 mi'
  },
  {
    id: 'TRP-2460',
    vehicle: 'FL-2103',
    driver: 'Robert Wilson',
    route: 'Miami → Orlando',
    status: 'delayed',
    duration: '3h 50m',
    distance: '235 mi'
  }
]

const pageSize = 3
const currentPage = ref(1)
const totalPages = computed(() => Math.max(1, Math.ceil(trips.length / pageSize)))
const safePage = computed(() => Math.min(currentPage.value, totalPages.value))
const pagedTrips = computed(() => {
  const start = (safePage.value - 1) * pageSize
  return trips.slice(start, start + pageSize)
})

const goToPage = (page) => {
  currentPage.value = Math.min(Math.max(1, page), totalPages.value)
}

const statusClass = (status) => {
  if (status === 'completed') return 'success'
  if (status === 'ongoing') return 'info'
  return 'warning'
}

const statusIcon = (status) => {
  if (status === 'completed') return 'mdi-check-circle-outline'
  if (status === 'ongoing') return 'mdi-timer-outline'
  return 'mdi-alert-circle-outline'
}

const formatStatus = (status) => status.charAt(0).toUpperCase() + status.slice(1)
</script>

<style scoped>
.table-header {
  padding: 20px;
  border-bottom: 1px solid var(--fleet-border);
}

.card-surface {
  overflow: hidden;
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

.table-base {
  width: max-content;
  min-width: 100%;
  border-collapse: collapse;
}

.table-base th,
.table-base td {
  white-space: nowrap;
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

.table-footer {
  padding: 12px 20px;
  border-top: 1px solid var(--fleet-border);
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

  .table-base {
    width: 100%;
    min-width: 760px;
  }

  .table-base th,
  .table-base td {
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
    padding: 16px;
  }

  .table-footer {
    padding: 12px 16px;
  }

  .table-wrap {
    overscroll-behavior-x: contain;
    scrollbar-gutter: stable both-edges;
    padding-bottom: 6px;
  }
}
</style>
