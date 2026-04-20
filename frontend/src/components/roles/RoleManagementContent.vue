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
        {{ loadingRoles ? 'Loading roles...' : `Showing ${filteredRoles.length} of ${roles.length} roles` }}
      </div>
    </div>

    <div v-if="pageError" class="page-error" role="alert">
      <span>{{ pageError }}</span>
      <button class="page-error-close" type="button" aria-label="Close error message" @click="pageError = ''">
        <v-icon icon="mdi-close" size="18" />
      </button>
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
      :loading="loadingMembers"
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
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import RoleTable from './RoleTable.vue'
import RoleDialog from './RoleDialog.vue'
import RoleMembersDialog from './RoleMembersDialog.vue'
import MemberAvatarDialog from './MemberAvatarDialog.vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'
import {
  createRole,
  deleteRole,
  getRoleMembers,
  getRoles,
  updateRole
} from '../../services/rolesApi'

const ALL_ROLES_FILTER = 'All'
const SEARCH_DELAY_MS = 350

const memberHeaders = [
  { title: 'Name', key: 'name' },
  { title: 'Email', key: 'email' },
  { title: 'Phone', key: 'phone' },
  { title: 'Status', key: 'status' },
  { title: 'Joined', key: 'joinDate' }
]

const SEARCHABLE_ROLE_FIELDS = ['name', 'description']
const SEARCHABLE_MEMBER_FIELDS = ['name', 'email', 'phone']

const normalizeText = (value) => String(value ?? '').toLowerCase()

const matchesSearch = (item, fields, query) =>
  !query || fields.some((field) => normalizeText(item[field]).includes(query))

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

const toRoleRequest = (payload) => ({
  name: payload.name,
  description: payload.description,
  status: payload.status || 'Active'
})

const roles = ref([])
const roleMembers = ref([])
const activeTab = ref(ALL_ROLES_FILTER)
const searchQuery = ref('')
const pageError = ref('')
const loadingRoles = ref(false)
const loadingMembers = ref(false)
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

const filteredRoles = computed(() => {
  const query = debouncedRoleQuery.value.toLowerCase()
  return roles.value.filter((role) => {
    const matchesTab = activeTab.value === ALL_ROLES_FILTER || role.name === activeTab.value
    return matchesTab && matchesSearch(role, SEARCHABLE_ROLE_FIELDS, query)
  })
})

const totalMembers = computed(() =>
  roles.value.reduce((total, role) => total + (role.members || 0), 0)
)
const driverMembers = computed(() => roles.value.find((role) => role.name === 'Driver')?.members || 0)
const adminMembers = computed(() => roles.value.find((role) => role.name === 'Admin')?.members || 0)

const filteredMembers = computed(() => {
  const query = debouncedMemberQuery.value.toLowerCase()
  return roleMembers.value.filter((member) =>
    matchesSearch(member, SEARCHABLE_MEMBER_FIELDS, query)
  )
})

const loadRoles = async () => {
  loadingRoles.value = true
  pageError.value = ''

  try {
    roles.value = await getRoles()
  } catch (error) {
    pageError.value = error.message
  } finally {
    loadingRoles.value = false
  }
}

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

const handleSave = async (payload) => {
  pageError.value = ''

  try {
    const savedRole =
      dialogMode.value === 'edit'
        ? await updateRole(payload.id, toRoleRequest(payload))
        : await createRole(toRoleRequest(payload))

    if (dialogMode.value === 'edit') {
      roles.value = roles.value.map((role) => (role.id === savedRole.id ? savedRole : role))
    } else {
      roles.value.unshift(savedRole)
    }

    dialogOpen.value = false
  } catch (error) {
    pageError.value = error.message
  }
}

const openMembers = async (role) => {
  selectedRole.value = role
  memberSearch.value = ''
  roleMembers.value = []
  membersOpen.value = true
  loadingMembers.value = true
  pageError.value = ''

  try {
    roleMembers.value = await getRoleMembers(role.id)
  } catch (error) {
    pageError.value = error.message
  } finally {
    loadingMembers.value = false
  }
}

const openConfirm = ({ title, message, confirmText, tone, action }) => {
  confirmTitle.value = title
  confirmMessage.value = message
  confirmButton.value = confirmText
  confirmTone.value = tone
  pendingAction.value = action
  confirmOpen.value = true
}

const runConfirm = async () => {
  await pendingAction.value()
  confirmOpen.value = false
}

const handleDelete = (role) => {
  openConfirm({
    title: 'Delete Role?',
    message: `This will permanently remove ${role.name}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: async () => {
      pageError.value = ''

      try {
        await deleteRole(role.id)
        roles.value = roles.value.filter((item) => item.id !== role.id)
      } catch (error) {
        pageError.value = error.message
      }
    }
  })
}

const openMemberAvatar = (member) => {
  if (!member?.avatar) return
  memberAvatarUrl.value = member.avatar
  memberAvatarName.value = member.name
  memberAvatarOpen.value = true
}

onMounted(loadRoles)
</script>

<style scoped src="./roles_styles/RoleManagementContent.css"></style>
