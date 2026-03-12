<template>
  <div class="user-page">
    <div>
      <h1 class="section-title">User Management</h1>
      <p class="section-subtitle">Manage system users and permissions</p>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Users</p>
        <h3>{{ users.length }}</h3>
      </div>
      <div class="stat-card">
        <p>Active Users</p>
        <h3 class="text-success">{{ activeCount }}</h3>
      </div>
      <div class="stat-card">
        <p>Drivers</p>
        <h3 class="text-info">{{ driverCount }}</h3>
      </div>
      <div class="stat-card">
        <p>Admins</p>
        <h3 class="text-purple">{{ adminCount }}</h3>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search by name or email..."
          />
        </div>

        <div class="toolbar-filter">
          <v-icon icon="mdi-filter-variant" />
          <select v-model="roleFilter">
            <option value="All">All Roles</option>
            <option value="Admin">Admin</option>
            <option value="Dispatcher">Dispatcher</option>
            <option value="Driver">Driver</option>
            <option value="Mechanic">Mechanic</option>
          </select>
        </div>

        <button class="primary-button" type="button" @click="dialogOpen = true">
          <v-icon icon="mdi-account-plus" size="18" />
          Add User
        </button>
      </div>

      <div class="toolbar-count text-muted">
        Showing {{ filteredUsers.length }} of {{ users.length }} users
      </div>
    </div>

    <UserTable
      :users="filteredUsers"
      @edit="handleEdit"
      @toggle="handleToggle"
      @remove="handleDelete"
    />

    <AddUserDialog
      :open="dialogOpen"
      @close="dialogOpen = false"
      @add="handleAdd"
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
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import UserTable from './UserTable.vue'
import AddUserDialog from './AddUserDialog.vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'

const users = ref([
  {
    id: '1',
    name: 'Sarah Johnson',
    email: 'sarah.johnson@fleet.com',
    role: 'Admin',
    status: 'Active',
    phone: '+1 (555) 123-4567',
    joinDate: '2024-01-15'
  },
  {
    id: '2',
    name: 'Michael Chen',
    email: 'michael.chen@fleet.com',
    role: 'Dispatcher',
    status: 'Active',
    phone: '+1 (555) 234-5678',
    joinDate: '2024-02-20'
  },
  {
    id: '3',
    name: 'John Martinez',
    email: 'john.martinez@fleet.com',
    role: 'Driver',
    status: 'Active',
    phone: '+1 (555) 345-6789',
    joinDate: '2023-11-10'
  },
  {
    id: '4',
    name: 'Emily Davis',
    email: 'emily.davis@fleet.com',
    role: 'Driver',
    status: 'Active',
    phone: '+1 (555) 456-7890',
    joinDate: '2024-03-05'
  },
  {
    id: '5',
    name: 'Robert Wilson',
    email: 'robert.wilson@fleet.com',
    role: 'Mechanic',
    status: 'Active',
    phone: '+1 (555) 567-8901',
    joinDate: '2023-09-12'
  },
  {
    id: '6',
    name: 'Jessica Brown',
    email: 'jessica.brown@fleet.com',
    role: 'Driver',
    status: 'Active',
    phone: '+1 (555) 678-9012',
    joinDate: '2024-01-28'
  },
  {
    id: '7',
    name: 'David Lee',
    email: 'david.lee@fleet.com',
    role: 'Dispatcher',
    status: 'Active',
    phone: '+1 (555) 789-0123',
    joinDate: '2023-12-05'
  },
  {
    id: '8',
    name: 'Amanda Taylor',
    email: 'amanda.taylor@fleet.com',
    role: 'Mechanic',
    status: 'Active',
    phone: '+1 (555) 890-1234',
    joinDate: '2024-02-14'
  },
  {
    id: '9',
    name: 'James Anderson',
    email: 'james.anderson@fleet.com',
    role: 'Driver',
    status: 'Disabled',
    phone: '+1 (555) 901-2345',
    joinDate: '2023-08-22'
  },
  {
    id: '10',
    name: 'Lisa Garcia',
    email: 'lisa.garcia@fleet.com',
    role: 'Admin',
    status: 'Active',
    phone: '+1 (555) 012-3456',
    joinDate: '2023-10-30'
  }
])

const searchQuery = ref('')
const roleFilter = ref('All')
const dialogOpen = ref(false)
const confirmOpen = ref(false)
const confirmTitle = ref('Are you sure?')
const confirmMessage = ref('')
const confirmButton = ref('Confirm')
const confirmTone = ref('danger')
const pendingAction = ref(() => {})

const filteredUsers = computed(() => {
  const query = searchQuery.value.toLowerCase()
  return users.value.filter((user) => {
    const matchesSearch =
      user.name.toLowerCase().includes(query) || user.email.toLowerCase().includes(query)
    const matchesRole = roleFilter.value === 'All' || user.role === roleFilter.value
    return matchesSearch && matchesRole
  })
})

const activeCount = computed(() => users.value.filter((u) => u.status === 'Active').length)
const driverCount = computed(() => users.value.filter((u) => u.role === 'Driver').length)
const adminCount = computed(() => users.value.filter((u) => u.role === 'Admin').length)

const handleAdd = (payload) => {
  users.value.push({
    ...payload,
    id: String(users.value.length + 1),
    joinDate: new Date().toISOString().split('T')[0]
  })
  dialogOpen.value = false
}

const handleEdit = (id) => {
  console.log('Edit user:', id)
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

const handleToggle = (id) => {
  const user = users.value.find((item) => item.id === id)
  if (!user) return
  const nextStatus = user.status === 'Active' ? 'Disabled' : 'Active'
  openConfirm({
    title: `${nextStatus} User?`,
    message: `This will mark ${user.name} as ${nextStatus.toLowerCase()}.`,
    confirmText: nextStatus,
    tone: 'warning',
    action: () => {
      users.value = users.value.map((item) =>
        item.id === id ? { ...item, status: nextStatus } : item
      )
    }
  })
}

const handleDelete = (id) => {
  const user = users.value.find((item) => item.id === id)
  if (!user) return
  openConfirm({
    title: 'Delete User?',
    message: `This will permanently remove ${user.name}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: () => {
      users.value = users.value.filter((item) => item.id !== id)
    }
  })
}
</script>

<style scoped>
.user-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
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

.text-info {
  color: var(--fleet-primary);
}

.text-purple {
  color: #7c3aed;
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

.toolbar-search,
.toolbar-filter {
  display: flex;
  align-items: center;
  gap: 8px;
  border: 1px solid var(--fleet-border);
  border-radius: 12px;
  padding: 10px 12px;
  background: #fff;
  min-width: 220px;
}

.toolbar-search input,
.toolbar-filter select {
  border: none;
  outline: none;
  background: transparent;
  font-size: 14px;
}

.toolbar-filter select {
  appearance: none;
}

.primary-button {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border: none;
  border-radius: 12px;
  padding: 10px 16px;
  background: var(--fleet-primary);
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}

.primary-button:hover {
  background: var(--fleet-primary-dark);
}

.toolbar-count {
  margin-top: 12px;
  font-size: 13px;
}
</style>
