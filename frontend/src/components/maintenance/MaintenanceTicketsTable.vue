<template>
  <div class="card-surface">
    <div class="table-wrap">
      <v-data-table-server
        class="table-base maintenance-table"
        :headers="headers"
        :items="items"
        :items-length="total"
        :loading="loading"
        :page="page"
        :items-per-page="itemsPerPage"
        :sort-by="[]"
        :items-per-page-options="[10, 20, 30]"
        :mobile-breakpoint="0"
        :mobile="false"
        fixed-header
        height="520"
        density="comfortable"
        @update:options="$emit('update:options', $event)"
      >
        <template #item.displayNumber="{ index }">
          <span class="text-muted">{{ rowNumber(index) }}</span>
        </template>

        <template #item.id="{ item }">
          <span class="text-muted">{{ item.id }}</span>
        </template>

        <template #item.vehicle="{ item }">
          <div class="vehicle-cell">
            <span class="vehicle-avatar">{{ initials(item.vehicle) }}</span>
            <div>
              <strong>{{ item.vehicle }}</strong>
              <div class="text-muted vehicle-sub">{{ item.vehicleId }}</div>
            </div>
          </div>
        </template>

        <template #item.issue="{ item }">
          <div class="issue-cell">
            <span class="issue-title">{{ item.issue }}</span>
            <span class="issue-note text-muted">{{ item.details }}</span>
          </div>
        </template>

        <template #item.reportedDate="{ item }">
          <span class="text-muted">{{ formatDate(item.reportedDate) }}</span>
        </template>

        <template #item.mechanic="{ item }">
          <div class="mechanic-cell">
            <span class="mechanic-avatar">{{ initials(item.mechanic) }}</span>
            <span>{{ item.mechanic }}</span>
          </div>
        </template>

        <template #item.status="{ item }">
          <span class="role-badge" :class="statusClass(item.status)">
            {{ item.status }}
          </span>
        </template>

        <template #item.actions="{ item }">
          <div class="inline-actions">
            <button v-if="canEdit" class="icon-button tooltip" type="button" @click="$emit('edit', item)">
              <v-icon icon="mdi-pencil-outline" size="18" />
              <span class="tooltip-text">Edit ticket</span>
            </button>
            <button
              class="icon-button tooltip"
              v-if="canEdit"
              :class="item.status === 'Completed' ? 'good' : 'warn'"
              type="button"
              @click="$emit('advance-status', item)"
            >
              <v-icon icon="mdi-progress-wrench" size="18" />
              <span class="tooltip-text">Advance status</span>
            </button>
            <button v-if="canDelete" class="icon-button danger tooltip" type="button" @click="$emit('remove', item)">
              <v-icon icon="mdi-trash-can-outline" size="18" />
              <span class="tooltip-text">Delete ticket</span>
            </button>
          </div>
        </template>

        <template #no-data>
          <div class="empty-state">No tickets found matching your criteria</div>
        </template>
      </v-data-table-server>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  items: {
    type: Array,
    required: true
  },
  total: {
    type: Number,
    default: 0
  },
  loading: {
    type: Boolean,
    default: false
  },
  itemsPerPage: {
    type: Number,
    default: 10
  },
  page: {
    type: Number,
    default: 1
  },
  sortBy: {
    type: String,
    default: 'reportedDate'
  },
  sortOrder: {
    type: String,
    default: 'desc'
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

defineEmits(['edit', 'advance-status', 'remove', 'update:options'])

const headers = [
  { title: 'No.', key: 'displayNumber', sortable: false },
  { title: 'Ticket ID', key: 'id', sortable: false },
  { title: 'Vehicle', key: 'vehicle', sortable: false },
  { title: 'Issue', key: 'issue', sortable: false },
  { title: 'Reported Date', key: 'reportedDate', sortable: false },
  { title: 'Mechanic', key: 'mechanic', sortable: false },
  { title: 'Status', key: 'status', sortable: false },
  { title: 'Actions', key: 'actions', align: 'end', sortable: false }
]

const rowNumber = (index) => {
  const currentPage = Math.max(Number(props.page) || 1, 1)
  const perPage = Math.max(Number(props.itemsPerPage) || 1, 1)
  const startIndex = (currentPage - 1) * perPage

  if (props.sortOrder === 'desc') {
    return Math.max(props.total - startIndex - index, 1)
  }

  return startIndex + index + 1
}

const initials = (name) => name.split(' ').map((part) => part[0]).join('')

const formatDate = (value) =>
  new Date(value).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })

const statusClass = (status) => {
  if (status === 'Completed') return 'role-driver'
  if (status === 'Repairing') return 'role-dispatcher'
  return 'role-mechanic'
}
</script>

<style scoped src="../roles/roles_styles/RoleTable.css"></style>

<style scoped>
.maintenance-table :deep(thead th:nth-child(1)),
.maintenance-table :deep(tbody td:nth-child(1)) {
  width: 90px;
}

.maintenance-table :deep(thead th:nth-child(2)),
.maintenance-table :deep(tbody td:nth-child(2)) {
  width: 140px;
}

.maintenance-table :deep(thead th:nth-child(3)),
.maintenance-table :deep(tbody td:nth-child(3)) {
  width: 240px;
}

.maintenance-table :deep(thead th:nth-child(4)),
.maintenance-table :deep(tbody td:nth-child(4)) {
  width: 300px;
}

.maintenance-table :deep(thead th:nth-child(5)),
.maintenance-table :deep(tbody td:nth-child(5)) {
  width: 160px;
}

.maintenance-table :deep(thead th:nth-child(6)),
.maintenance-table :deep(tbody td:nth-child(6)) {
  width: 180px;
}

.maintenance-table :deep(thead th:nth-child(7)),
.maintenance-table :deep(tbody td:nth-child(7)) {
  width: 140px;
}

.maintenance-table :deep(thead th:nth-child(8)),
.maintenance-table :deep(tbody td:nth-child(8)) {
  width: 150px;
}

.vehicle-cell,
.mechanic-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.vehicle-avatar,
.mechanic-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #2563eb, #1d4ed8);
  color: #fff;
  font-size: 12px;
  font-weight: 700;
  flex: 0 0 36px;
}

.vehicle-sub,
.issue-note {
  font-size: 12px;
}

.issue-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.issue-title {
  font-weight: 600;
}
</style>
