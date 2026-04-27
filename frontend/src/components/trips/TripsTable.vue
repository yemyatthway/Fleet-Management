<template>
  <div class="card-surface table-card">
    <div class="table-wrap">
      <v-data-table
        class="table-base trips-table"
        :headers="headers"
        :items="items"
        :page="page"
        :items-per-page="itemsPerPage"
        :items-per-page-options="[8, 16, 24]"
        :mobile-breakpoint="0"
        :mobile="false"
        fixed-header
        height="560"
        density="comfortable"
        @update:options="$emit('update:options', $event)"
      >
        <template #item.displayId="{ index }">
          <span class="row-id">{{ rowNumber(index) }}</span>
        </template>

        <template #item.tripNumber="{ item }">
          <div class="trip-id-cell">
            <strong class="trip-number">{{ item.tripNumber }}</strong>
            <div class="text-muted trip-sub">{{ item.tripType }} • {{ item.priority }}</div>
          </div>
        </template>

        <template #item.schedule="{ item }">
          <div class="stack-cell">
            <strong class="schedule-primary">{{ formatDateTime(item.departureDateTime) }}</strong>
            <span class="text-muted">ETA {{ formatDateTime(item.estimatedArrival) }}</span>
          </div>
        </template>

        <template #item.route="{ item }">
          <div class="route-cell">
            <div class="route-line">
              <v-icon icon="mdi-map-marker-radius-outline" size="18" />
              <span>{{ item.pickupLocation }}</span>
            </div>
            <div class="route-arrow text-muted">to {{ item.dropoffLocation }}</div>
          </div>
        </template>

        <template #item.vehicle="{ item }">
          <div class="stack-cell">
            <strong class="vehicle-plate">{{ item.vehiclePlate }}</strong>
            <span class="text-muted">
              {{ item.vehicleId }}<span v-if="item.trailerNumber"> • {{ item.trailerNumber }}</span>
            </span>
          </div>
        </template>

        <template #item.driver="{ item }">
          <div class="stack-cell">
            <strong>{{ item.driverName }}</strong>
            <span class="text-muted">{{ item.dispatcherName }}</span>
          </div>
        </template>

        <template #item.load="{ item }">
          <div class="stack-cell">
            <strong>{{ item.cargoType }}</strong>
            <span class="text-muted">
              {{ item.loadWeightKg.toLocaleString() }} kg • {{ item.plannedDistanceKm }} km
            </span>
          </div>
        </template>

        <template #item.status="{ item }">
          <span class="badge" :class="statusClass(item.status)">{{ item.status }}</span>
        </template>

        <template #item.actions="{ item }">
          <div class="inline-actions">
            <button class="icon-button tooltip" type="button" @click="$emit('view', item)">
              <v-icon icon="mdi-eye-outline" size="18" />
              <span class="tooltip-text">View details</span>
            </button>
            <button class="icon-button tooltip" type="button" @click="$emit('edit', item)">
              <v-icon icon="mdi-pencil-outline" size="18" />
              <span class="tooltip-text">Edit trip</span>
            </button>
            <button class="icon-button danger tooltip" type="button" @click="$emit('remove', item.id)">
              <v-icon icon="mdi-trash-can-outline" size="18" />
              <span class="tooltip-text">Delete trip</span>
            </button>
          </div>
        </template>

        <template #no-data>
          <div class="empty-state">No trips found matching your filters</div>
        </template>
      </v-data-table>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  items: {
    type: Array,
    required: true
  },
  page: {
    type: Number,
    default: 1
  },
  itemsPerPage: {
    type: Number,
    default: 8
  }
})

defineEmits(['update:options', 'view', 'edit', 'remove'])

const headers = [
  { title: 'No.', key: 'displayId', sortable: false },
  { title: 'Trip', key: 'tripNumber', sortable: false },
  { title: 'Schedule', key: 'schedule', sortable: false },
  { title: 'Route', key: 'route', sortable: false },
  { title: 'Vehicle', key: 'vehicle', sortable: false },
  { title: 'Driver', key: 'driver', sortable: false },
  { title: 'Load', key: 'load', sortable: false },
  { title: 'Status', key: 'status', sortable: false },
  { title: 'Actions', key: 'actions', sortable: false, align: 'end', width: 140 }
]

const rowNumber = (index) => (props.page - 1) * props.itemsPerPage + index + 1

const statusClass = (status) => {
  if (status === 'Completed') return 'success'
  if (status === 'In Transit') return 'info'
  if (status === 'Delayed') return 'warning'
  if (status === 'Cancelled') return 'danger'
  return 'neutral'
}

const formatDateTime = (value) => {
  if (!value) return '—'

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value

  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit'
  }).format(date)
}
</script>
