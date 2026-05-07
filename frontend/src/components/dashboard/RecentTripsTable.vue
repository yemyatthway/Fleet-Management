<template>
  <div class="recent-trips-card">
    <div class="table-header">
      <div>
        <h2 class="table-title">{{ title }}</h2>
        <p class="table-subtitle">{{ subtitle }}</p>
      </div>
    </div>
    <div class="table-wrap">
      <v-data-table
        v-model:page="currentPage"
        v-model:items-per-page="itemsPerPage"
        class="table-base recent-trips-table"
        :headers="headers"
        :items="trips"
        :items-per-page-options="[5, 10, 20]"
        :mobile-breakpoint="0"
        :mobile="false"
        fixed-header
        height="420"
        density="comfortable"
      >
        <template #item.tripNumber="{ item }">
          <strong>{{ item.tripNumber || item.id }}</strong>
        </template>

        <template #item.vehicle="{ item }">
          <span class="text-muted">{{ item.vehicle || '-' }}</span>
        </template>

        <template #item.driver="{ item }">
          <span>{{ item.driver || '-' }}</span>
        </template>

        <template #item.route="{ item }">
          <div class="route">
            <v-icon icon="mdi-map-marker" size="18" />
            <span class="text-muted">{{ item.route || '-' }}</span>
          </div>
        </template>

        <template #item.status="{ item }">
          <span class="badge" :class="statusClass(item.status)">
            <v-icon :icon="statusIcon(item.status)" size="14" />
            {{ formatStatus(item.status) }}
          </span>
        </template>

        <template #item.details="{ item }">
          <span class="text-muted">{{ item.details || `${item.duration || '-'} • ${item.distance || '-'}` }}</span>
        </template>

        <template #no-data>
          <div class="empty-cell">{{ emptyText }}</div>
        </template>
      </v-data-table>
    </div>
    <div class="table-footer">
      <button v-if="linkTo" class="link-button" type="button" @click="goToLink">{{ linkLabel }}</button>
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

const currentPage = ref(1)
const itemsPerPage = ref(5)
const router = useRouter()
const trips = computed(() => props.trips)
const headers = computed(() => [
  { title: props.firstColumnLabel, key: 'tripNumber', sortable: false },
  { title: 'Vehicle', key: 'vehicle', sortable: false },
  { title: 'Driver', key: 'driver', sortable: false },
  { title: 'Route', key: 'route', sortable: false },
  { title: 'Status', key: 'status', sortable: false },
  { title: 'Details', key: 'details', sortable: false }
])

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

<style scoped src="./dashboard_styles/RecentTripsTable.css"></style>
