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

        <template #item.code="{ item }">
          <span class="text-muted">{{ item.code }}</span>
        </template>

        <template #item.description="{ item }">
          <span class="text-muted">{{ item.description || '—' }}</span>
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
              <span class="tooltip-text">Edit fuel type</span>
            </button>
            <button class="icon-button danger tooltip" type="button" @click="$emit('remove', item)">
              <v-icon icon="mdi-trash-can-outline" size="18" />
              <span class="tooltip-text">Delete fuel type</span>
            </button>
          </div>
        </template>

        <template #item.createdAt="{ item }">
          <span class="text-muted">{{ formatDate(item.createdAt || item.updatedAt) }}</span>
        </template>

        <template #no-data>
          <div class="empty-state">No fuel types found</div>
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
  { title: 'Description', key: 'description', sortable: false },
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
