<template>
  <div class="card-surface">
    <div class="table-wrap">
      <v-data-table-server
        class="table-base"
        :headers="headers"
        :items="items"
        :items-length="total"
        :loading="loading"
        :items-per-page="itemsPerPage"
        :items-per-page-options="[10, 20, 30]"
        :mobile-breakpoint="0"
        :mobile="false"
        fixed-header
        height="520"
        density="comfortable"
        @update:options="$emit('update:options', $event)"
      >
        <template #item.type="{ item }">
          <div class="role-cell">
            <div class="role-badge" :class="item.type === 'Department' ? 'role-dispatcher' : 'role-driver'">
              {{ item.type === 'Location' ? 'Location / Depot' : item.type }}
            </div>
          </div>
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
              <span class="tooltip-text">Edit code</span>
            </button>
            <button class="icon-button danger tooltip" type="button" @click="$emit('remove', item)">
              <v-icon icon="mdi-trash-can-outline" size="18" />
              <span class="tooltip-text">Delete code</span>
            </button>
          </div>
        </template>

        <template #item.createdAt="{ item }">
          <span class="text-muted">{{ formatDate(item.createdAt || item.updatedAt) }}</span>
        </template>

        <template #no-data>
          <div class="empty-state">No code setup records found</div>
        </template>
      </v-data-table-server>
    </div>
  </div>
</template>

<script setup>
defineProps({
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
  }
})

defineEmits(['edit', 'remove', 'update:options'])

const headers = [
  { title: 'Type', key: 'type' },
  { title: 'Name', key: 'name' },
  { title: 'Description', key: 'description' },
  { title: 'Status', key: 'status' },
  { title: 'Actions', key: 'actions', align: 'end', sortable: false },
  { title: 'Created At', key: 'createdAt' }
]

const formatDate = (value) =>
  new Date(value).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })
</script>

<style scoped src="../roles/roles_styles/RoleTable.css"></style>
