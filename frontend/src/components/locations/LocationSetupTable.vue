<template>
  <div class="card-surface">
    <div class="table-wrap">
      <v-data-table-server
        class="table-base"
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
        <template #item.displayId="{ index }">
          <span class="text-muted">{{ rowNumber(index) }}</span>
        </template>

        <template #item.type="{ item }">
          <div class="role-cell">
            <div class="role-badge role-driver">{{ item.type }}</div>
          </div>
        </template>

        <template #item.code="{ item }">
          <span class="text-muted">{{ item.code || '—' }}</span>
        </template>

        <template #item.city="{ item }">
          <span class="text-muted">{{ item.city || '—' }}</span>
        </template>

        <template #item.country="{ item }">
          <span class="text-muted">{{ item.country || '—' }}</span>
        </template>

        <template #item.contactPerson="{ item }">
          <span class="text-muted">{{ item.contactPerson || '—' }}</span>
        </template>

        <template #item.phone="{ item }">
          <span class="text-muted">{{ item.phone || '—' }}</span>
        </template>

        <template #item.notes="{ item }">
          <span class="text-muted notes-cell">{{ item.notes || '—' }}</span>
        </template>

        <template #item.status="{ item }">
          <div class="role-cell">
            <div class="role-badge" :class="item.status === 'Active' ? 'role-admin' : 'role-mechanic'">
              {{ item.status }}
            </div>
          </div>
        </template>

        <template #item.actions="{ item }">
          <div class="inline-actions">
            <button class="icon-button tooltip" type="button" @click="$emit('edit', item)">
              <v-icon icon="mdi-pencil-outline" size="18" />
              <span class="tooltip-text">Edit location</span>
            </button>
            <button class="icon-button danger tooltip" type="button" @click="$emit('remove', item)">
              <v-icon icon="mdi-trash-can-outline" size="18" />
              <span class="tooltip-text">Delete location</span>
            </button>
          </div>
        </template>

        <template #item.createdAt="{ item }">
          <span class="text-muted">{{ formatDate(item.createdAt || item.updatedAt) }}</span>
        </template>

        <template #no-data>
          <div class="empty-state">No locations found</div>
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
    default: 'id'
  },
  sortOrder: {
    type: String,
    default: 'asc'
  }
})

defineEmits(['edit', 'remove', 'update:options'])

const headers = [
  { title: 'No.', key: 'displayId', sortable: false },
  { title: 'Name', key: 'name', sortable: false },
  { title: 'Code', key: 'code', sortable: false },
  { title: 'Type', key: 'type', sortable: false },
  { title: 'City', key: 'city', sortable: false },
  { title: 'Country', key: 'country', sortable: false },
  { title: 'Contact Person', key: 'contactPerson', sortable: false },
  { title: 'Phone', key: 'phone', sortable: false },
  { title: 'Notes', key: 'notes', sortable: false },
  { title: 'Status', key: 'status', sortable: false },
  { title: 'Actions', key: 'actions', align: 'end', sortable: false },
  { title: 'Created At', key: 'createdAt', sortable: false }
]

const rowNumber = (index) => {
  const safePage = Math.max(Number(props.page) || 1, 1)
  const safeItemsPerPage = Math.max(Number(props.itemsPerPage) || 1, 1)
  const safeTotal = Math.max(Number(props.total) || 0, 0)
  const startIndex = (safePage - 1) * safeItemsPerPage
  const descending = String(props.sortOrder || '').toLowerCase() === 'desc'
  const value = descending
    ? Math.max(safeTotal - startIndex - index, 1)
    : startIndex + index + 1

  return String(value).padStart(3, '0')
}

const formatDate = (value) =>
  new Date(value).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })
</script>

<style scoped src="../roles/roles_styles/RoleTable.css"></style>

<style scoped>
.notes-cell {
  display: -webkit-box;
  max-width: 220px;
  overflow: hidden;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}
</style>
