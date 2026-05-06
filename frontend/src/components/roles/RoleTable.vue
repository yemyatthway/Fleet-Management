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
        :sort-by="[]"
        :items-per-page-options="[10, 20, 30]"
        :mobile-breakpoint="0"
        :mobile="false"
        fixed-header
        height="520"
        density="comfortable"
        @update:options="$emit('update:options', $event)"
      >
        <template #item.code="{ item }">
          <span class="text-muted">{{ item.code }}</span>
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
import { roleClassFor } from '../../utils/roleClasses'

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
    default: 'code'
  },
  sortOrder: {
    type: String,
    default: 'asc'
  }
})

defineEmits(['view', 'update:options'])

const headers = [
  { title: 'ID', key: 'code', sortable: false },
  { title: 'Role', key: 'name', sortable: false },
  { title: 'Description', key: 'description', sortable: false },
  { title: 'Members', key: 'members', sortable: false },
  { title: 'View', key: 'view', align: 'end', sortable: false },
  { title: 'Created At', key: 'createdAt', sortable: false }
]

const roleClass = (role) => roleClassFor(role)

const formatDate = (value) =>
  new Date(value).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })
</script>

<style scoped src="./roles_styles/RoleTable.css"></style>
