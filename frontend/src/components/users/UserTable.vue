<template>
  <div class="card-surface">
    <div class="table-wrap">
      <v-data-table-server
        class="table-base"
        :headers="headers"
        :items="users"
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
          <span class="user-id">{{ rowNumber(index) }}</span>
        </template>

        <template #item.name="{ item }">
          <div class="name-cell">
            <button
              class="avatar avatar-button tooltip"
              type="button"
              @click="item.avatar && $emit('view-avatar', item)"
            >
              <img v-if="item.avatar" :src="item.avatar" :alt="item.name" loading="lazy" decoding="async" />
              <span v-else>{{ initials(item.name) }}</span>
              <span v-if="item.avatar" class="tooltip-text">View profile image</span>
            </button>
            <strong>{{ item.name }}</strong>
          </div>
        </template>

        <template #item.nrcNumber="{ item }">
          <span class="text-muted">{{ item.nrcNumber }}</span>
        </template>

        <template #item.nrcFront="{ item }">
          <button
            v-if="item.nrcFront"
            class="document-thumb tooltip"
            type="button"
            @click="$emit('view-avatar', { name: `${item.name} - NRC Front`, avatar: item.nrcFront })"
          >
            <img :src="item.nrcFront" :alt="`${item.name} NRC front`" loading="lazy" decoding="async" />
            <span class="tooltip-text">View NRC front</span>
          </button>
          <span v-else class="text-muted">—</span>
        </template>

        <template #item.nrcBack="{ item }">
          <button
            v-if="item.nrcBack"
            class="document-thumb tooltip"
            type="button"
            @click="$emit('view-avatar', { name: `${item.name} - NRC Back`, avatar: item.nrcBack })"
          >
            <img :src="item.nrcBack" :alt="`${item.name} NRC back`" loading="lazy" decoding="async" />
            <span class="tooltip-text">View NRC back</span>
          </button>
          <span v-else class="text-muted">—</span>
        </template>

        <template #item.email="{ item }">
          <span class="text-muted">{{ item.email }}</span>
        </template>

        <template #item.role="{ item }">
          <span class="role-pill" :class="roleClass(item.role)">{{ item.role }}</span>
        </template>

        <template #item.phone="{ item }">
          <span class="text-muted no-wrap-cell">{{ item.phone }}</span>
        </template>

        <template #item.department="{ item }">
          <span class="text-muted">{{ item.department }}</span>
        </template>

        <template #item.title="{ item }">
          <span class="text-muted">{{ item.title }}</span>
        </template>

        <template #item.location="{ item }">
          <span class="text-muted">{{ item.location }}</span>
        </template>

        <template #item.manager="{ item }">
          <span class="text-muted">{{ item.manager }}</span>
        </template>

        <template #item.status="{ item }">
          <span class="badge" :class="item.status === 'Active' ? 'success' : 'neutral'">
            {{ item.status }}
          </span>
        </template>

        <template #item.joinDate="{ item }">
          <span class="text-muted no-wrap-cell">{{ formatDate(item.joinDate) }}</span>
        </template>

        <template #item.lastLogin="{ item }">
          <span class="text-muted no-wrap-cell">{{ formatDateTime(item.lastLogin) }}</span>
        </template>

        <template #item.twoFactorEnabled="{ item }">
          <span class="badge" :class="item.twoFactorEnabled ? 'success' : 'neutral'">
            {{ item.twoFactorEnabled ? 'Enabled' : 'Off' }}
          </span>
        </template>

        <template #item.actions="{ item }">
          <div class="inline-actions">
            <button v-if="canEdit" class="icon-button tooltip" @click="$emit('edit', item.id)">
              <v-icon icon="mdi-pencil-outline" size="18" />
              <span class="tooltip-text">Edit user</span>
            </button>
            <button
              v-if="canEdit"
              class="icon-button tooltip"
              :class="item.status === 'Active' ? 'warn' : 'good'"
              @click="$emit('toggle', item.id)"
            >
              <v-icon icon="mdi-power" size="18" />
              <span class="tooltip-text">
                {{ item.status === 'Active' ? 'Disable user' : 'Enable user' }}
              </span>
            </button>
            <button v-if="canDelete" class="icon-button danger tooltip" @click="$emit('remove', item.id)">
              <v-icon icon="mdi-trash-can-outline" size="18" />
              <span class="tooltip-text">Delete user</span>
            </button>
          </div>
        </template>

        <template #no-data>
          <div class="empty-state">No users found matching your criteria</div>
        </template>
      </v-data-table-server>
    </div>
  </div>
</template>

<script setup>
import { roleClassMap } from '../../data/roles'

const props = defineProps({
  users: {
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
    default: 'name'
  },
  sortOrder: {
    type: String,
    default: 'asc'
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

defineEmits(['edit', 'toggle', 'remove', 'view-avatar', 'update:options'])

const headers = [
  { title: 'No.', key: 'displayId', sortable: false },
  { title: 'Name', key: 'name', sortable: false },
  { title: 'NRC', key: 'nrcNumber', sortable: false },
  { title: 'NRC Front', key: 'nrcFront', sortable: false },
  { title: 'NRC Back', key: 'nrcBack', sortable: false },
  { title: 'Email', key: 'email', sortable: false },
  { title: 'Role', key: 'role', sortable: false },
  { title: 'Phone', key: 'phone', sortable: false },
  { title: 'Department', key: 'department', sortable: false },
  { title: 'Title', key: 'title', sortable: false },
  { title: 'Location', key: 'location', sortable: false },
  { title: 'Manager', key: 'manager', sortable: false },
  { title: 'Status', key: 'status', sortable: false },
  { title: 'Join Date', key: 'joinDate', sortable: false },
  { title: 'Last Login', key: 'lastLogin', sortable: false },
  { title: '2FA', key: 'twoFactorEnabled', sortable: false },
  { title: 'Actions', key: 'actions', align: 'end', sortable: false }
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

const roleClass = (role) => roleClassMap[role] || 'role-driver'

const initials = (name) => name.split(' ').map((part) => part[0]).join('')

const formatDate = (value) =>
  new Date(value).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })

const formatDateTime = (value) => {
  if (!value) return '—'
  return new Date(value).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true
  })
}
</script>

<style scoped src="./users_styles/UserTable.css"></style>

<style scoped>
.no-wrap-cell {
  white-space: nowrap;
}
</style>
