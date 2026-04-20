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

    <RoleMembersDialog
      :open="membersOpen"
      :role="selectedRole"
      :members="filteredMembers"
      :headers="memberHeaders"
      :search="memberSearch"
      @update:open="membersOpen = $event"
      @update:search="memberSearch = $event"
      @view-avatar="openMemberAvatar"
    />

    <MemberAvatarDialog
      :open="memberAvatarOpen"
      :name="memberAvatarName"
      :url="memberAvatarUrl"
      @update:open="memberAvatarOpen = $event"
    />
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import RoleTable from './RoleTable.vue'
import RoleDialog from './RoleDialog.vue'
import RoleMembersDialog from './RoleMembersDialog.vue'
import MemberAvatarDialog from './MemberAvatarDialog.vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'
import { roleCatalog } from '../../data/roles'
import { users } from '../../data/users'

const ALL_ROLES_FILTER = 'All'
const ACTIVE_STATUS = 'Active'
const DEFAULT_ROLE_CREATED_AT = '2024-03-01'
const SEARCH_DELAY_MS = 350

const roleSeed = [
  {
    name: 'Admin',
    permissions: ['Full access', 'Manage users', 'View reports', 'Edit settings'],
    status: ACTIVE_STATUS,
    createdAt: '2024-03-18'
  },
  {
    name: 'Dispatcher',
    permissions: ['Assign routes', 'Monitor trips', 'Manage drivers'],
    status: ACTIVE_STATUS,
    createdAt: '2024-02-28'
  },
  {
    name: 'Driver',
    permissions: ['View schedule', 'Update status', 'Log issues'],
    status: ACTIVE_STATUS,
    createdAt: DEFAULT_ROLE_CREATED_AT
  },
  {
    name: 'Mechanic',
    permissions: ['Maintenance tickets', 'Update inspections', 'Log repairs'],
    status: 'Disabled',
    createdAt: '2024-01-22'
  }
]

const memberHeaders = [
  { title: 'Name', key: 'name' },
  { title: 'Email', key: 'email' },
  { title: 'Phone', key: 'phone' },
  { title: 'Status', key: 'status' },
  { title: 'Joined', key: 'joinDate' }
]

const ROLE_SEED_BY_NAME = new Map(roleSeed.map((role) => [role.name, role]))
const SEARCHABLE_ROLE_FIELDS = ['name', 'description']
const SEARCHABLE_MEMBER_FIELDS = ['name', 'email', 'phone']

const todayIsoDate = () => new Date().toISOString().split('T')[0]

const normalizeText = (value) => String(value ?? '').toLowerCase()

const createRoleId = (name) => normalizeText(name).trim().replace(/\s+/g, '-')

const matchesSearch = (item, fields, query) =>
  !query || fields.some((field) => normalizeText(item[field]).includes(query))

const createRoleRecord = (role) => {
  const seed = ROLE_SEED_BY_NAME.get(role.name)

  return {
    id: role.id,
    name: role.name,
    description: role.description,
    permissions: seed?.permissions || [],
    status: seed?.status || ACTIVE_STATUS,
    createdAt: seed?.createdAt || DEFAULT_ROLE_CREATED_AT
  }
}

const createRoleFromPayload = (payload) => ({
  ...payload,
  id: createRoleId(payload.name),
  permissions: [],
  createdAt: todayIsoDate()
})

const mergeRoleUpdate = (role, payload) =>
  role.id === payload.id
    ? {
        ...role,
        ...payload,
        permissions: role.permissions || [],
        updatedAt: todayIsoDate()
      }
    : role

const countUsersByRole = (userList) =>
  userList.reduce((counts, user) => {
    counts.set(user.role, (counts.get(user.role) || 0) + 1)
    return counts
  }, new Map())

const useDebouncedRef = (source, delay = SEARCH_DELAY_MS) => {
  const debounced = ref(source.value)
  let timerId = null

  const clearTimer = () => {
    if (timerId) clearTimeout(timerId)
  }

  watch(
    source,
    (value) => {
      clearTimer()
      timerId = setTimeout(() => {
        debounced.value = value
      }, delay)
    },
    { immediate: true }
  )

  onBeforeUnmount(clearTimer)

  return debounced
}

const roles = ref(roleCatalog.map(createRoleRecord))
const activeTab = ref(ALL_ROLES_FILTER)
const searchQuery = ref('')
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
const memberAvatarOpen = ref(false)
const memberAvatarUrl = ref('')
const memberAvatarName = ref('')

const debouncedRoleQuery = useDebouncedRef(searchQuery)
const debouncedMemberQuery = useDebouncedRef(memberSearch)

const roleTabs = computed(() => [...new Set(roles.value.map((role) => role.name))])
const userRoleCounts = computed(() => countUsersByRole(users.value))

const rolesWithMembers = computed(() => {
  return roles.value.map((role) => ({
    ...role,
    members: userRoleCounts.value.get(role.name) || 0
  }))
})

const filteredRoles = computed(() => {
  const query = debouncedRoleQuery.value.toLowerCase()
  return rolesWithMembers.value.filter((role) => {
    const matchesTab = activeTab.value === ALL_ROLES_FILTER || role.name === activeTab.value
    return matchesTab && matchesSearch(role, SEARCHABLE_ROLE_FIELDS, query)
  })
})

const totalMembers = computed(() => users.value.length)
const driverMembers = computed(() => userRoleCounts.value.get('Driver') || 0)
const adminMembers = computed(() => userRoleCounts.value.get('Admin') || 0)

const roleMembers = computed(() => {
  if (!selectedRole.value) return []

  return users.value.filter(
    (user) => user.role === selectedRole.value.name && user.status === ACTIVE_STATUS
  )
})

const filteredMembers = computed(() => {
  const query = debouncedMemberQuery.value.toLowerCase()
  return roleMembers.value.filter((member) =>
    matchesSearch(member, SEARCHABLE_MEMBER_FIELDS, query)
  )
})

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
    roles.value = roles.value.map((role) => mergeRoleUpdate(role, payload))
  } else {
    roles.value.unshift(createRoleFromPayload(payload))
  }

  dialogOpen.value = false
}

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
</script>

<style scoped src="./roles_styles/RoleManagementContent.css"></style>



