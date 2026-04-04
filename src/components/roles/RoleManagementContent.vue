<template>
  <div class="role-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Role Management</h1>
        <p class="section-subtitle">Define access levels and operational ownership</p>
      </div>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Roles</p>
        <h3>{{ roles.length }}</h3>
      </div>
      <div class="stat-card">
        <p>Assigned Users</p>
        <h3 class="text-info">{{ totalMembers }}</h3>
      </div>
      <div class="stat-card">
        <p>Driver Roles</p>
        <h3 class="text-success">{{ driverMembers }}</h3>
      </div>
      <div class="stat-card">
        <p>Admins</p>
        <h3 class="text-purple">{{ adminMembers }}</h3>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input v-model="searchQuery" type="text" placeholder="Search roles or descriptions..." />
          <button
            v-if="searchQuery"
            class="clear-button"
            type="button"
            aria-label="Clear search"
            @click="searchQuery = ''"
          >
            <v-icon icon="mdi-close-circle" size="18" />
          </button>
        </div>

        <div class="toolbar-filter">
          <v-icon icon="mdi-filter-variant" />
          <select v-model="activeTab">
            <option value="All">All Roles</option>
            <option v-for="role in roleTabs" :key="role" :value="role">
              {{ role }}
            </option>
          </select>
        </div>

        <button class="primary-button" type="button" @click="openAdd">
          <v-icon icon="mdi-shield-plus" size="18" />
          Create Role
        </button>
      </div>

      <div class="toolbar-count text-muted">
        Showing {{ filteredRoles.length }} of {{ roles.length }} roles
      </div>
    </div>

    <RoleTable :roles="filteredRoles" @view="openMembers" @edit="openEdit" @remove="handleDelete" />

    <RoleDialog
      :open="dialogOpen"
      :mode="dialogMode"
      :role="selectedRole"
      @close="dialogOpen = false"
      @save="handleSave"
    />

    <ConfirmDialog
      :open="confirmOpen"
      :title="confirmTitle"
      :message="confirmMessage"
      :confirm-text="confirmButton"
      :tone="confirmTone"
      @confirm="runConfirm"
      @cancel="confirmOpen = false"
    />

    <v-dialog v-model="membersOpen" max-width="820">
      <v-card class="dialog-card">
        <div class="dialog-header">
          <div>
            <h2>{{ selectedRole?.name }} Members</h2>
            <p class="text-muted">{{ filteredMembers.length }} members</p>
          </div>
          <button class="icon-button" type="button" @click="membersOpen = false">
            <v-icon icon="mdi-close" />
          </button>
        </div>

        <div class="dialog-body">
          <div class="toolbar-search">
            <v-icon icon="mdi-magnify" />
          <input v-model="memberSearch" type="text" placeholder="Search members..." />
          <button
            v-if="memberSearch"
            class="clear-button"
            type="button"
            aria-label="Clear member search"
            @click="memberSearch = ''"
          >
            <v-icon icon="mdi-close-circle" size="18" />
          </button>
        </div>

          <div class="card-surface table-card">
            <div class="table-wrap">
              <v-data-table
                class="table-base"
                :headers="memberHeaders"
                :items="filteredMembers"
                :items-per-page="10"
                :items-per-page-options="[10, 20, 30]"
                :mobile-breakpoint="0"
                :mobile="false"
                fixed-header
                height="360"
                density="comfortable"
              >
                <template #item.name="{ item }">
                  <div class="name-cell">
                    <button
                      class="avatar avatar-button tooltip"
                      type="button"
                      @click="item.avatar && openMemberAvatar(item)"
                    >
                      <img v-if="item.avatar" :src="item.avatar" :alt="item.name" />
                      <span v-else>{{ initials(item.name) }}</span>
                      <span v-if="item.avatar" class="tooltip-text">View profile image</span>
                    </button>
                    <strong>{{ item.name }}</strong>
                  </div>
                </template>

                <template #item.email="{ item }">
                  <span class="text-muted">{{ item.email }}</span>
                </template>

                <template #item.phone="{ item }">
                  <span class="text-muted">{{ item.phone }}</span>
                </template>

                <template #item.status="{ item }">
                  <span class="badge" :class="item.status === 'Active' ? 'success' : 'neutral'">
                    {{ item.status }}
                  </span>
                </template>

                <template #item.joinDate="{ item }">
                  <span class="text-muted">{{ formatDate(item.joinDate) }}</span>
                </template>

                <template #no-data>
                  <div class="empty-state">No members found for this role</div>
                </template>
              </v-data-table>
            </div>
          </div>
        </div>
      </v-card>
    </v-dialog>

    <v-dialog v-model="memberAvatarOpen" max-width="420">
      <v-card class="dialog-card">
        <div class="dialog-header">
          <h2>{{ memberAvatarName }}</h2>
          <button class="icon-button" type="button" @click="memberAvatarOpen = false">
            <v-icon icon="mdi-close" />
          </button>
        </div>
        <div class="dialog-body">
          <img v-if="memberAvatarUrl" class="avatar-preview" :src="memberAvatarUrl" :alt="memberAvatarName" />
        </div>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import RoleTable from './RoleTable.vue'
import RoleDialog from './RoleDialog.vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'
import { roleCatalog } from '../../data/roles'
import { users } from '../../data/users'

const roleSeed = [
  {
    name: 'Admin',
    permissions: ['Full access', 'Manage users', 'View reports', 'Edit settings'],
    status: 'Active',
    createdAt: '2024-03-18'
  },
  {
    name: 'Dispatcher',
    permissions: ['Assign routes', 'Monitor trips', 'Manage drivers'],
    status: 'Active',
    createdAt: '2024-02-28'
  },
  {
    name: 'Driver',
    permissions: ['View schedule', 'Update status', 'Log issues'],
    status: 'Active',
    createdAt: '2024-03-01'
  },
  {
    name: 'Mechanic',
    permissions: ['Maintenance tickets', 'Update inspections', 'Log repairs'],
    status: 'Disabled',
    createdAt: '2024-01-22'
  }
]

const roles = ref(
  roleCatalog.map((role) => {
    const seed = roleSeed.find((item) => item.name === role.name)
    return {
      id: role.id,
      name: role.name,
      description: role.description,
      permissions: seed?.permissions || [],
      createdAt: seed?.createdAt || '2024-03-01'
    }
  })
)

const roleTabs = computed(() => [...new Set(roles.value.map((role) => role.name))])
const activeTab = ref('All')
const searchQuery = ref('')
const debouncedRoleQuery = ref('')
const dialogOpen = ref(false)
const dialogMode = ref('add')
const selectedRole = ref(null)
const confirmOpen = ref(false)
const confirmTitle = ref('Are you sure?')
const confirmMessage = ref('')
const confirmButton = ref('Confirm')
const confirmTone = ref('danger')
const pendingAction = ref(() => {})
const membersOpen = ref(false)
const memberSearch = ref('')
const debouncedMemberQuery = ref('')
const memberAvatarOpen = ref(false)
const memberAvatarUrl = ref('')
const memberAvatarName = ref('')

const rolesWithMembers = computed(() => {
  const counts = users.value.reduce((acc, user) => {
    acc[user.role] = (acc[user.role] || 0) + 1
    return acc
  }, {})
  return roles.value.map((role) => ({
    ...role,
    members: counts[role.name] || 0
  }))
})

const filteredRoles = computed(() => {
  const query = debouncedRoleQuery.value.toLowerCase()
  return rolesWithMembers.value.filter((role) => {
    const matchesSearch =
      role.name.toLowerCase().includes(query) ||
      role.description.toLowerCase().includes(query)
    const matchesTab = activeTab.value === 'All' || role.name === activeTab.value
    return matchesSearch && matchesTab
  })
})

const totalMembers = computed(() => users.value.length)
const driverMembers = computed(() => users.value.filter((user) => user.role === 'Driver').length)
const adminMembers = computed(() => users.value.filter((user) => user.role === 'Admin').length)

const openAdd = () => {
  dialogMode.value = 'add'
  selectedRole.value = null
  dialogOpen.value = true
}

const openEdit = (role) => {
  dialogMode.value = 'edit'
  selectedRole.value = { ...role }
  dialogOpen.value = true
}

const handleSave = (payload) => {
  if (dialogMode.value === 'edit') {
    roles.value = roles.value.map((role) =>
      role.id === payload.id
        ? { ...role, ...payload, permissions: role.permissions || [], updatedAt: new Date().toISOString().split('T')[0] }
        : role
    )
  } else {
    const id = payload.name.toLowerCase().replace(/\s+/g, '-')
    roles.value.unshift({
      ...payload,
      id,
      permissions: [],
      createdAt: new Date().toISOString().split('T')[0]
    })
  }
  dialogOpen.value = false
}

const roleMembers = computed(() => {
  if (!selectedRole.value) return []
  return users.value.filter(
    (user) => user.role === selectedRole.value.name && user.status === 'Active'
  )
})

const filteredMembers = computed(() => {
  const query = debouncedMemberQuery.value.toLowerCase()
  return roleMembers.value.filter((member) => {
    return (
      member.name.toLowerCase().includes(query) ||
      member.email.toLowerCase().includes(query) ||
      member.phone.toLowerCase().includes(query)
    )
  })
})

let roleSearchTimer = null
watch(
  () => searchQuery.value,
  (value) => {
    if (roleSearchTimer) clearTimeout(roleSearchTimer)
    roleSearchTimer = setTimeout(() => {
      debouncedRoleQuery.value = value
    }, 350)
  },
  { immediate: true }
)

let memberSearchTimer = null
watch(
  () => memberSearch.value,
  (value) => {
    if (memberSearchTimer) clearTimeout(memberSearchTimer)
    memberSearchTimer = setTimeout(() => {
      debouncedMemberQuery.value = value
    }, 350)
  },
  { immediate: true }
)

onBeforeUnmount(() => {
  if (roleSearchTimer) clearTimeout(roleSearchTimer)
  if (memberSearchTimer) clearTimeout(memberSearchTimer)
})


const openMembers = (role) => {
  selectedRole.value = role
  memberSearch.value = ''
  membersOpen.value = true
}

const openConfirm = ({ title, message, confirmText, tone, action }) => {
  confirmTitle.value = title
  confirmMessage.value = message
  confirmButton.value = confirmText
  confirmTone.value = tone
  pendingAction.value = action
  confirmOpen.value = true
}

const runConfirm = () => {
  pendingAction.value()
  confirmOpen.value = false
}

const handleDelete = (role) => {
  openConfirm({
    title: 'Delete Role?',
    message: `This will permanently remove ${role.name}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: () => {
      roles.value = roles.value.filter((item) => item.id !== role.id)
    }
  })
}

const openMemberAvatar = (member) => {
  if (!member?.avatar) return
  memberAvatarUrl.value = member.avatar
  memberAvatarName.value = member.name
  memberAvatarOpen.value = true
}

const initials = (name) => name.split(' ').map((part) => part[0]).join('')

const formatDate = (value) =>
  new Date(value).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })

const memberHeaders = [
  { title: 'Name', key: 'name' },
  { title: 'Email', key: 'email' },
  { title: 'Phone', key: 'phone' },
  { title: 'Status', key: 'status' },
  { title: 'Joined', key: 'joinDate' }
]
</script>

<style scoped>
.role-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  flex-wrap: wrap;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 16px;
}

.stat-card {
  padding: 16px;
  border-radius: 14px;
  border: 1px solid var(--fleet-border);
  background: #fff;
}

.stat-card p {
  margin: 0;
  font-size: 13px;
  color: var(--fleet-muted);
}

.stat-card h3 {
  margin: 8px 0 0;
  font-size: 22px;
}

.text-success {
  color: var(--fleet-success);
}

.text-purple {
  color: #7c3aed;
}

.text-info {
  color: var(--fleet-primary);
}

.role-tabs {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  padding: 12px;
}

.tab-button {
  border: 1px solid var(--fleet-border);
  background: #fff;
  color: #334155;
  padding: 8px 14px;
  border-radius: 999px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.tab-button.active {
  background: #2563eb;
  border-color: #2563eb;
  color: #fff;
}

.tab-button:hover {
  border-color: #94a3b8;
}

.toolbar {
  padding: 18px;
}

.toolbar-row {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
}

.toolbar-search {
  display: flex;
  align-items: center;
  gap: 8px;
  border: 1px solid var(--fleet-border);
  border-radius: 12px;
  padding: 10px 12px;
  background: #fff;
  min-width: 260px;
  flex: 1;
}

.toolbar-filter {
  display: flex;
  align-items: center;
  gap: 8px;
  border: 1px solid var(--fleet-border);
  border-radius: 12px;
  padding: 10px 12px;
  background: #fff;
  min-width: 220px;
  cursor: pointer;
}

.toolbar-filter select {
  border: none;
  outline: none;
  background: transparent;
  font-size: 14px;
  width: 100%;
  appearance: none;
  cursor: pointer;
}

.toolbar-search input {
  border: none;
  outline: none;
  background: transparent;
  font-size: 14px;
  width: 100%;
}

.clear-button {
  border: none;
  background: transparent;
  color: #94a3b8;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
}

.clear-button:hover {
  color: #475569;
}

.primary-button {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border: none;
  border-radius: 12px;
  padding: 10px 16px;
  background: linear-gradient(135deg, #2563eb, #1d4ed8);
  color: #fff;
  font-weight: 600;
  cursor: pointer;
  box-shadow: 0 10px 18px rgba(37, 99, 235, 0.25);
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.primary-button:hover {
  transform: translateY(-1px);
  box-shadow: 0 14px 24px rgba(37, 99, 235, 0.3);
}

.primary-button:active {
  transform: translateY(0);
  box-shadow: 0 6px 12px rgba(37, 99, 235, 0.2);
}

.toolbar-count {
  margin-top: 12px;
  font-size: 13px;
}

@media (max-width: 720px) {
  .toolbar-row {
    flex-direction: column;
    align-items: stretch;
  }

  .toolbar-search {
    width: 100%;
  }

  .toolbar-filter {
    width: 100%;
  }

  .primary-button {
    width: 100%;
    justify-content: center;
  }
}

.dialog-card {
  border-radius: 16px;
  padding: 0;
}

.dialog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px;
  border-bottom: 1px solid var(--fleet-border);
}

.dialog-header h2 {
  margin: 0;
  font-size: 18px;
}

.dialog-header p {
  margin: 4px 0 0;
  font-size: 13px;
}

.dialog-body {
  display: flex;
  flex-direction: column;
  gap: 16px;
  padding: 20px 24px 24px;
}

.toolbar-search {
  display: flex;
  align-items: center;
  gap: 8px;
  border: 1px solid var(--fleet-border);
  border-radius: 12px;
  padding: 10px 12px;
  background: #fff;
}

.toolbar-search input {
  border: none;
  outline: none;
  background: transparent;
  font-size: 14px;
}

.table-base :deep(.v-table__wrapper) {
  background: #fff;
}

.table-base :deep(table) {
  border-collapse: separate;
  border-spacing: 0;
}

.table-base :deep(thead th) {
  background: #f8fafc;
  color: #475569;
  font-size: 13px;
  letter-spacing: 0.02em;
  text-transform: uppercase;
  font-weight: 700;
  padding: 14px 16px;
}

.table-base :deep(tbody td) {
  padding: 14px 16px;
  background: #fff;
}

.table-base :deep(tbody tr) {
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.06);
}

.table-base :deep(tbody tr td) {
  border-bottom: 10px solid transparent;
}

.table-base :deep(tbody tr:last-child td) {
  border-bottom: 0;
}

.table-base :deep(tbody tr:nth-child(even) td) {
  background: #f8fafc;
}

.table-base :deep(tbody tr td:first-child) {
  border-radius: 12px 0 0 12px;
}

.table-base :deep(tbody tr td:last-child) {
  border-radius: 0 12px 12px 0;
}

.table-base :deep(thead th:first-child) {
  border-radius: 12px 0 0 12px;
}

.table-base :deep(thead th:last-child) {
  border-radius: 0 12px 12px 0;
}

.name-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.avatar {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-weight: 700;
  color: #fff;
  background: linear-gradient(135deg, #2563eb, #1e40af);
  overflow: hidden;
  border: none;
  padding: 0;
}

.avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.avatar-button {
  cursor: pointer;
}

.avatar-button:disabled {
  cursor: default;
}

.icon-button {
  border: none;
  background: transparent;
  cursor: pointer;
  border-radius: 10px;
  width: 36px;
  height: 36px;
}

.icon-button:hover {
  background: #f1f5f9;
}

.tooltip {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}

.tooltip:focus-visible {
  outline: 2px solid rgba(37, 99, 235, 0.35);
  outline-offset: 2px;
}

.tooltip-text {
  position: absolute;
  bottom: calc(100% + 8px);
  left: 0;
  transform: translate(0, 6px);
  background: #0f172a;
  color: #fff;
  padding: 6px 8px;
  border-radius: 8px;
  font-size: 12px;
  white-space: nowrap;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.15s ease, transform 0.15s ease;
  box-shadow: 0 8px 16px rgba(15, 23, 42, 0.2);
  z-index: 2;
}

.tooltip:hover .tooltip-text,
.tooltip:focus-visible .tooltip-text {
  opacity: 1;
  transform: translate(0, 0);
}
</style>
