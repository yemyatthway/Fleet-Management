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
          <tr v-for="trip in trips" :key="trip.id">
            <td><strong>{{ trip.id }}</strong></td>
            <td class="text-muted">{{ trip.vehicle }}</td>
            <td>{{ trip.driver }}</td>
            <td>
              <div class="route">
                <v-icon icon="mdi-map-marker" size="18" />
                <span class="text-muted">{{ trip.route }}</span>
              </div>
            </td>
            <td>
              <span class="badge" :class="statusClass(trip.status)">
                <v-icon :icon="statusIcon(trip.status)" size="14" />
                {{ formatStatus(trip.status) }}
              </span>
            </td>
            <td class="text-muted">{{ trip.duration }} • {{ trip.distance }}</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div class="table-footer">
      <button class="link-button" type="button">View All Trips →</button>
    </div>
  </div>
</template>

<script setup>
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
}

.route {
  display: flex;
  align-items: center;
  gap: 6px;
}

.table-footer {
  padding: 12px 20px;
  border-top: 1px solid var(--fleet-border);
  text-align: center;
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
</style>
