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
        <h3>{{ totalRoles }}</h3>
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
        {{ loadingRoles ? 'Loading roles...' : `Showing ${roles.length} of ${totalRoles} roles` }}
      </div>
    </div>

    <PageMessage
      :tone="pageMessage.tone"
      :title="pageMessage.title"
      :message="pageMessage.message"
      @close="clearPageMessage"
    />

    <RoleTable
      :roles="tableRoles"
      :total="totalRoles"
      :loading="loadingRoles"
      :page="tableOptions.page"
      :items-per-page="tableOptions.itemsPerPage"
      :sort-by="tableOptions.sortBy"
      :sort-order="tableOptions.sortOrder"
      @update:options="handleTableOptions"
      @view="openMembers"
      @edit="openEdit"
      @remove="handleDelete"
    />

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
import PageMessage from '../common/PageMessage.vue'
import { attachDisplayIds } from '../../utils/tableDisplayIds'
import {
  createRole,
  deleteRole,
  getRoleMembers,
  getRoles,
  updateRole
} from '../../services/rolesApi'

const ALL_ROLES_FILTER = 'All'
const SEARCH_DELAY_MS = 350
const PAGE_MESSAGE_DURATION_MS = 5000

const memberHeaders = [
  { title: 'Name', key: 'name' },
  { title: 'Email', key: 'email' },
  { title: 'Phone', key: 'phone' },
  { title: 'Status', key: 'status' },
  { title: 'Joined', key: 'joinDate' }
]

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
const totalRoles = ref(0)
const tableOptions = ref({ page: 1, itemsPerPage: 10, sortBy: 'id', sortOrder: 'asc' })
const pageMessage = ref({ tone: 'info', title: '', message: '' })
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
let pageMessageTimerId = null

const debouncedRoleQuery = useDebouncedRef(searchQuery)
const debouncedMemberQuery = useDebouncedRef(memberSearch)

const roleTabs = computed(() => [...new Set(roles.value.map((role) => role.name))])

const totalMembers = computed(() =>
  roles.value.reduce((total, role) => total + (role.members || 0), 0)
)
const driverMembers = computed(() => roles.value.find((role) => role.name === 'Driver')?.members || 0)
const adminMembers = computed(() => roles.value.find((role) => role.name === 'Admin')?.members || 0)
const tableRoles = computed(() =>
  attachDisplayIds(
    roles.value,
    tableOptions.value.page,
    tableOptions.value.itemsPerPage,
    true,
    () => 'ROL'
  )
)

const filteredMembers = computed(() => {
  const query = debouncedMemberQuery.value.toLowerCase()
  return roleMembers.value.filter((member) =>
    matchesSearch(member, SEARCHABLE_MEMBER_FIELDS, query)
  )
})

const clearPageMessage = () => {
  if (pageMessageTimerId) {
    clearTimeout(pageMessageTimerId)
    pageMessageTimerId = null
  }
  pageMessage.value = { tone: 'info', title: '', message: '' }
}

const showPageMessage = ({ tone = 'info', title = '', message }) => {
  if (pageMessageTimerId) clearTimeout(pageMessageTimerId)
  pageMessage.value = { tone, title, message }
  pageMessageTimerId = setTimeout(() => {
    pageMessageTimerId = null
    clearPageMessage()
  }, PAGE_MESSAGE_DURATION_MS)
}

const loadRoles = async () => {
  loadingRoles.value = true
  clearPageMessage()

  try {
    const result = await getRoles({
      page: tableOptions.value.page,
      pageSize: tableOptions.value.itemsPerPage,
      search: debouncedRoleQuery.value,
      sortBy: tableOptions.value.sortBy,
      sortOrder: tableOptions.value.sortOrder
    })
    roles.value = result.items || []
    totalRoles.value = result.total || 0
  } catch (error) {
    showPageMessage({ tone: 'error', title: 'Could not load roles', message: error.message })
  } finally {
    loadingRoles.value = false
  }
}

const normalizeSortOption = (sortBy) => {
  const firstSort = sortBy?.[0]
  if (!firstSort) return null
  if (typeof firstSort === 'string') return { key: firstSort, order: 'asc' }

  const key = firstSort.key || firstSort.field || ''
  const order =
    firstSort.order ||
    (typeof firstSort.desc === 'boolean' ? (firstSort.desc ? 'desc' : 'asc') : 'asc')

  return key ? { key, order } : null
}

const handleTableOptions = (options) => {
  const firstSort = normalizeSortOption(options.sortBy)
  tableOptions.value = {
    page: options.page || 1,
    itemsPerPage: options.itemsPerPage || 10,
    sortBy: firstSort?.key || tableOptions.value.sortBy || 'id',
    sortOrder: firstSort?.order || tableOptions.value.sortOrder || 'asc'
  }
  loadRoles()
}

watch(debouncedRoleQuery, () => {
  tableOptions.value.page = 1
  loadRoles()
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

const handleSave = async (payload) => {
  clearPageMessage()
  const isEdit = dialogMode.value === 'edit'

  try {
    const savedRole =
      isEdit
        ? await updateRole(payload.id, toRoleRequest(payload))
        : await createRole(toRoleRequest(payload))

    if (isEdit) {
      roles.value = roles.value.map((role) => (role.id === savedRole.id ? savedRole : role))
    } else {
      await loadRoles()
    }

    dialogOpen.value = false
    showPageMessage({
      tone: 'success',
      title: isEdit ? 'Role updated' : 'Role created',
      message: `${savedRole.name} has been ${isEdit ? 'updated' : 'created'} successfully.`
    })
  } catch (error) {
    showPageMessage({ tone: 'error', title: 'Role was not saved', message: error.message })
  }
}

const openMembers = async (role) => {
  selectedRole.value = role
  memberSearch.value = ''
  roleMembers.value = []
  membersOpen.value = true
  loadingMembers.value = true
  clearPageMessage()

  try {
    roleMembers.value = await getRoleMembers(role.id)
  } catch (error) {
    showPageMessage({ tone: 'error', title: 'Could not load members', message: error.message })
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
      clearPageMessage()

      try {
        await deleteRole(role.id)
        await loadRoles()
        showPageMessage({
          tone: 'warning',
          title: 'Role deleted',
          message: `${role.name} has been removed.`
        })
      } catch (error) {
        showPageMessage({ tone: 'error', title: 'Role was not deleted', message: error.message })
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

onBeforeUnmount(() => {
  if (pageMessageTimerId) clearTimeout(pageMessageTimerId)
})
</script>

<style scoped src="./roles_styles/RoleManagementContent.css"></style>
