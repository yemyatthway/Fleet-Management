<template>
  <div class="card-surface table-card">
    <div class="table-wrap">
      <v-data-table
        class="table-base trips-table"
        :headers="headers"
        :items="items"
        :page="page"
        :items-per-page="itemsPerPage"
        :items-per-page-options="[10, 20, 30]"
        :mobile-breakpoint="0"
        :mobile="false"
        fixed-header
        height="520"
        density="comfortable"
        @update:options="$emit('update:options', $event)"
      >
        <template #item.displayId="{ index }">
          <span class="row-id">{{ rowNumber(index) }}</span>
        </template>

        <template #item.tripNumber="{ item }">
          <div class="trip-id-cell">
            <strong class="trip-number">{{ item.tripNumber }}</strong>
            <div class="text-muted trip-sub">{{ item.priority }}</div>
          </div>
        </template>

        <template #item.tripType="{ item }">
          <span class="type-pill">{{ item.tripType || '—' }}</span>
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

        <template #item.status="{ item }">
          <span class="badge" :class="statusClass(item.status)">{{ item.status }}</span>
        </template>

        <template #item.actions="{ item }">
          <div class="inline-actions">
            <button class="icon-button tooltip" type="button" @click="$emit('view', item)">
              <v-icon icon="mdi-eye-outline" size="18" />
              <span class="tooltip-text">View details</span>
            </button>
            <button v-if="canEdit" class="icon-button tooltip" type="button" @click="$emit('edit', item)">
              <v-icon icon="mdi-pencil-outline" size="18" />
              <span class="tooltip-text">Edit trip</span>
            </button>
            <button v-if="canDelete" class="icon-button danger tooltip" type="button" @click="$emit('remove', item.id)">
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
    default: 10
  },
  canEdit: {
    type: Boolean,
    default: false
  },
  canDelete: {
    type: Boolean,
    default: false
  }
})

defineEmits(['update:options', 'view', 'edit', 'remove'])

const headers = [
  { title: 'No.', key: 'displayId', sortable: false },
  { title: 'Trip', key: 'tripNumber', sortable: false },
  { title: 'Trip Type', key: 'tripType', sortable: false },
  { title: 'Route', key: 'route', sortable: false },
  { title: 'Vehicle', key: 'vehicle', sortable: false },
  { title: 'Driver', key: 'driver', sortable: false },
  { title: 'Status', key: 'status', sortable: false },
  { title: 'Actions', key: 'actions', sortable: false, align: 'end', width: 140 }
]

const rowNumber = (index) => {
  const value = (props.page - 1) * props.itemsPerPage + index + 1
  return String(value).padStart(3, '0')
}

const statusClass = (status) => {
  if (status === 'Completed') return 'success'
  if (status === 'In Transit') return 'info'
  if (status === 'Delayed') return 'warning'
  if (status === 'Cancelled') return 'danger'
  return 'neutral'
}
</script>
