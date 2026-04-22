<template>
  <div class="card-surface">
    <div class="table-wrap">
      <v-data-table-server
        class="table-base"
        :headers="headers"
        :items="roles"
        :items-length="total"
        :loading="loading"
        :page="page"
        :items-per-page="itemsPerPage"
        :sort-by="normalizedSortBy"
        :items-per-page-options="[10, 20, 30]"
        :mobile-breakpoint="0"
        :mobile="false"
        fixed-header
        height="520"
        density="comfortable"
        @update:options="$emit('update:options', $event)"
      >
        <template #item.id="{ item }">
          <span class="text-muted">{{ item.displayId }}</span>
        </template>

        <template #item.name="{ item }">
          <div class="role-cell">
            <div class="role-badge" :class="roleClass(item.name)">{{ item.name }}</div>
          </div>
        </template>

        <template #item.description="{ item }">
          <span class="text-muted">{{ item.description }}</span>
        </template>


        <template #item.members="{ item }">
          <strong>{{ item.members }}</strong>
        </template>

        <template #item.view="{ item }">
          <button class="icon-button tooltip" type="button" @click="$emit('view', item)">
            <v-icon icon="mdi-eye-outline" size="18" />
            <span class="tooltip-text">View members</span>
          </button>
        </template>

        <template #item.actions="{ item }">
          <div class="inline-actions">
            <button class="icon-button tooltip" type="button" @click="$emit('edit', item)">
              <v-icon icon="mdi-pencil-outline" size="18" />
              <span class="tooltip-text">Edit role</span>
            </button>
            <button class="icon-button danger tooltip" type="button" @click="$emit('remove', item)">
              <v-icon icon="mdi-trash-can-outline" size="18" />
              <span class="tooltip-text">Delete role</span>
            </button>
          </div>
        </template>

        <template #item.createdAt="{ item }">
          <span class="text-muted">{{ formatDate(item.createdAt || item.updatedAt) }}</span>
        </template>

        <template #no-data>
          <div class="empty-state">No roles found matching your criteria</div>
        </template>
      </v-data-table-server>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { roleClassMap } from '../../data/roles'

const props = defineProps({
  roles: {
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

defineEmits(['view', 'edit', 'remove', 'update:options'])

const headers = [
  { title: 'ID', key: 'id' },
  { title: 'Role', key: 'name' },
  { title: 'Description', key: 'description' },
  { title: 'Members', key: 'members' },
  { title: 'View', key: 'view', align: 'end', sortable: false },
  { title: 'Actions', key: 'actions', align: 'end', sortable: false },
  { title: 'Created At', key: 'createdAt' }
]

const normalizedSortBy = computed(() => [
  {
    key: props.sortBy,
    order: props.sortOrder
  }
])

const roleClass = (role) => roleClassMap[role] || 'role-driver'

const formatDate = (value) =>
  new Date(value).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })
</script>

<style scoped src="./roles_styles/RoleTable.css"></style>
