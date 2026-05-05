<template>
  <DashboardLayout>
    <div class="permission-page">
      <div class="page-header">
        <div>
          <h1 class="section-title">Permission Management</h1>
          <p class="section-subtitle">Control page access and actions for each fixed system role.</p>
        </div>
        <button class="primary-button" type="button" :disabled="saving || loading" @click="savePermissions">
          <v-icon icon="mdi-content-save-outline" size="18" />
          Save Permissions
        </button>
      </div>

      <div class="stats-grid">
        <div class="stat-card">
          <p>Roles</p>
          <h3>{{ roles.length }}</h3>
        </div>
        <div class="stat-card">
          <p>Modules</p>
          <h3 class="text-info">{{ modules.length }}</h3>
        </div>
        <div class="stat-card">
          <p>Enabled Rules</p>
          <h3 class="text-success">{{ enabledCount }}</h3>
        </div>
        <div class="stat-card">
          <p>Categories</p>
          <h3 class="text-purple">{{ categories.length }}</h3>
        </div>
      </div>

      <PageMessage
        :tone="pageMessage.tone"
        :title="pageMessage.title"
        :message="pageMessage.message"
        @close="clearPageMessage"
      />

      <div class="card-surface toolbar">
        <div class="toolbar-row">
          <div class="toolbar-search">
            <v-icon icon="mdi-magnify" />
            <input v-model="searchQuery" type="text" placeholder="Search modules or categories..." />
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
            <select v-model="categoryFilter">
              <option value="All">All Categories</option>
              <option v-for="category in categories" :key="category" :value="category">
                {{ category }}
              </option>
            </select>
          </div>
        </div>

        <div class="toolbar-count text-muted">
          {{ loading ? 'Loading permissions...' : `Showing ${filteredModules.length} of ${modules.length} modules` }}
        </div>
      </div>

      <div class="card-surface matrix-card">
        <div class="matrix-scroll">
          <table class="permission-table">
            <thead>
              <tr>
                <th class="module-col">Module</th>
                <th v-for="role in roles" :key="role.id" class="role-col">
                  {{ role.name }}
                </th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="!loading && !filteredModules.length">
                <td class="empty-state" :colspan="roles.length + 1">No modules found</td>
              </tr>
              <tr v-for="module in filteredModules" :key="module.key">
                <td class="module-cell">
                  <strong>{{ module.name }}</strong>
                  <span>{{ module.category }}</span>
                </td>
                <td v-for="role in roles" :key="`${module.key}-${role.id}`">
                  <div class="permission-switches">
                    <label v-for="action in actions" :key="action.key" class="permission-toggle">
                      <input
                        type="checkbox"
                        :checked="getPermission(module.key, role.id, action.key)"
                        @change="setPermission(module.key, role.id, action.key, $event.target.checked)"
                      />
                      <span>{{ action.label }}</span>
                    </label>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </DashboardLayout>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import DashboardLayout from '../layouts/DashboardLayout.vue'
import PageMessage from '../components/common/PageMessage.vue'
import { usePageMessage } from '../composables/usePageMessage'
import { getPermissions, updatePermissions } from '../services/permissionsApi'

const actions = [
  { key: 'canView', label: 'View' },
  { key: 'canCreate', label: 'Create' },
  { key: 'canEdit', label: 'Edit' },
  { key: 'canDelete', label: 'Delete' }
]

const roles = ref([])
const modules = ref([])
const loading = ref(false)
const saving = ref(false)
const searchQuery = ref('')
const categoryFilter = ref('All')
const { pageMessage, clearPageMessage, showPageMessage } = usePageMessage()

const categories = computed(() => [...new Set(modules.value.map((module) => module.category))])

const filteredModules = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()
  return modules.value.filter((module) => {
    const matchesCategory = categoryFilter.value === 'All' || module.category === categoryFilter.value
    const matchesSearch =
      !query ||
      module.name.toLowerCase().includes(query) ||
      module.category.toLowerCase().includes(query)
    return matchesCategory && matchesSearch
  })
})

const enabledCount = computed(() =>
  modules.value.reduce(
    (total, module) =>
      total +
      module.permissions.reduce(
        (permissionTotal, permission) =>
          permissionTotal +
          actions.filter((action) => Boolean(permission[action.key])).length,
        0
      ),
    0
  )
)

const findPermission = (moduleKey, roleId) => {
  const module = modules.value.find((item) => item.key === moduleKey)
  return module?.permissions.find((permission) => permission.roleId === roleId)
}

const getPermission = (moduleKey, roleId, action) =>
  Boolean(findPermission(moduleKey, roleId)?.[action])

const setPermission = (moduleKey, roleId, action, value) => {
  const permission = findPermission(moduleKey, roleId)
  if (!permission) return
  permission[action] = value
}

const loadPermissions = async () => {
  loading.value = true
  clearPageMessage()
  try {
    const matrix = await getPermissions()
    roles.value = matrix.roles || []
    modules.value = matrix.modules || []
  } catch (error) {
    showPageMessage({
      tone: 'error',
      title: 'Could not load permissions',
      message: error.message
    })
  } finally {
    loading.value = false
  }
}

const toPermissionPayload = () =>
  modules.value.flatMap((module) =>
    module.permissions.map((permission) => ({
      roleId: permission.roleId,
      moduleKey: module.key,
      canView: Boolean(permission.canView),
      canCreate: Boolean(permission.canCreate),
      canEdit: Boolean(permission.canEdit),
      canDelete: Boolean(permission.canDelete)
    }))
  )

const savePermissions = async () => {
  saving.value = true
  clearPageMessage()
  try {
    const matrix = await updatePermissions(toPermissionPayload())
    roles.value = matrix.roles || []
    modules.value = matrix.modules || []
    showPageMessage({
      tone: 'success',
      title: 'Permissions saved',
      message: 'Role permissions have been updated.'
    })
  } catch (error) {
    showPageMessage({
      tone: 'error',
      title: 'Permissions were not saved',
      message: error.message
    })
  } finally {
    saving.value = false
  }
}

onMounted(loadPermissions)
</script>

<style scoped src="../components/roles/roles_styles/RoleManagementContent.css"></style>

<style scoped>
.permission-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.page-header {
  align-items: flex-start;
}

.primary-button:disabled {
  opacity: 0.55;
  cursor: not-allowed;
}

.matrix-card {
  overflow: hidden;
  padding: 0;
}

.matrix-scroll {
  overflow: auto;
}

.permission-table {
  width: 100%;
  min-width: 1180px;
  border-collapse: collapse;
}

.permission-table th,
.permission-table td {
  padding: 16px;
  border-bottom: 1px solid var(--fleet-border);
  vertical-align: top;
}

.permission-table th {
  position: sticky;
  top: 0;
  z-index: 1;
  background: #f8fafc;
  color: #475569;
  font-size: 12px;
  text-align: left;
  text-transform: uppercase;
}

.module-col {
  width: 240px;
}

.role-col {
  width: 220px;
}

.module-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.module-cell strong {
  color: #0f172a;
  font-size: 14px;
}

.module-cell span {
  color: var(--fleet-muted);
  font-size: 12px;
}

.permission-switches {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px;
}

.permission-toggle {
  display: flex;
  align-items: center;
  gap: 7px;
  color: #334155;
  font-size: 12px;
  font-weight: 700;
}

.permission-toggle input {
  width: 16px;
  height: 16px;
  accent-color: #2563eb;
}

.empty-state {
  padding: 32px;
  color: var(--fleet-muted);
  text-align: center;
}

@media (max-width: 900px) {
  .page-header {
    flex-direction: column;
  }
}
</style>
