<template>
  <DashboardLayout>
    <main class="records-page">
      <header class="records-header">
        <div>
          <h1>Audit Logs</h1>
          <p>Review create, edit, delete, and status-change activity across modules.</p>
        </div>
      </header>

      <div v-if="pageError" class="page-error">{{ pageError }}</div>

      <section class="toolbar">
        <select v-model="mode" @change="refreshRecords">
          <option value="audit">Audit Logs</option>
          <option value="status">Status History</option>
        </select>
        <input v-if="mode === 'audit'" v-model="filters.module" placeholder="Module key" @input="refreshRecords" />
        <input v-if="mode === 'status'" v-model="filters.entityType" placeholder="Entity type" @input="refreshRecords" />
        <input v-if="mode === 'status'" v-model="filters.entityId" placeholder="Entity ID" @input="refreshRecords" />
      </section>

      <section class="table-card">
        <div class="table-wrap">
          <v-data-table-server
            v-model:page="pagination.page"
            v-model:items-per-page="pagination.pageSize"
            class="table-base audit-data-table"
            :headers="tableHeaders"
            :items="records"
            :items-length="totalRecords"
            :items-per-page-options="[10, 20, 30]"
            :loading="loading"
            :mobile-breakpoint="0"
            :mobile="false"
            fixed-header
            height="620"
            density="comfortable"
            @update:options="handleTableOptions"
          >
            <template #item.createdAt="{ item }">
              <span class="text-muted">{{ formatDate(item.createdAt) }}</span>
            </template>

            <template #item.roleId="{ item }">
              <span class="role-badge" :class="roleClass(item.roleId)">{{ item.roleId }}</span>
            </template>

            <template #item.moduleKey="{ item }">
              <span class="module-key">{{ item.moduleKey }}</span>
            </template>

            <template #item.action="{ item }">
              <span class="role-badge" :class="actionClass(item.action)">{{ item.action }}</span>
            </template>

            <template #item.oldStatus="{ item }">
              <span class="text-muted">{{ item.oldStatus || '-' }}</span>
            </template>

            <template #item.newStatus="{ item }">
              <span class="role-badge" :class="statusClass(item.newStatus)">{{ item.newStatus }}</span>
            </template>

            <template #no-data>
              <div class="empty-cell">{{ mode === 'audit' ? 'No audit logs found' : 'No status history found' }}</div>
            </template>
          </v-data-table-server>
        </div>
      </section>
    </main>
  </DashboardLayout>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import DashboardLayout from '../layouts/DashboardLayout.vue'
import { getAuditLogs, getStatusHistory } from '../services/auditApi'

const mode = ref('audit')
const records = ref([])
const totalRecords = ref(0)
const loading = ref(false)
const pageError = ref('')
const filters = reactive({ module: '', entityType: '', entityId: '' })
const pagination = reactive({ page: 1, pageSize: 10 })

const loadRecords = async () => {
  pageError.value = ''
  loading.value = true
  try {
    const result = mode.value === 'audit'
      ? await getAuditLogs({ module: filters.module, page: pagination.page, pageSize: pagination.pageSize })
      : await getStatusHistory({ entityType: filters.entityType, entityId: filters.entityId, page: pagination.page, pageSize: pagination.pageSize })

    records.value = result?.items || []
    totalRecords.value = result?.total || 0

    const maxPage = Math.max(1, Math.ceil(totalRecords.value / pagination.pageSize))
    if (pagination.page > maxPage) {
      pagination.page = maxPage
      const retry = mode.value === 'audit'
        ? await getAuditLogs({ module: filters.module, page: pagination.page, pageSize: pagination.pageSize })
        : await getStatusHistory({ entityType: filters.entityType, entityId: filters.entityId, page: pagination.page, pageSize: pagination.pageSize })
      records.value = retry?.items || []
      totalRecords.value = retry?.total || 0
    }
  } catch (error) {
    records.value = []
    totalRecords.value = 0
    pageError.value = error.message || 'Could not load audit records.'
  } finally {
    loading.value = false
  }
}

const totalPages = computed(() => Math.max(1, Math.ceil(totalRecords.value / pagination.pageSize)))
const tableHeaders = computed(() =>
  mode.value === 'audit'
    ? [
        { title: 'Time', key: 'createdAt', sortable: false },
        { title: 'Role', key: 'roleId', sortable: false },
        { title: 'Module', key: 'moduleKey', sortable: false },
        { title: 'Action', key: 'action', sortable: false },
        { title: 'Entity', key: 'entityId', sortable: false },
        { title: 'Description', key: 'description', sortable: false }
      ]
    : [
        { title: 'Time', key: 'createdAt', sortable: false },
        { title: 'Role', key: 'roleId', sortable: false },
        { title: 'Entity Type', key: 'entityType', sortable: false },
        { title: 'Entity ID', key: 'entityId', sortable: false },
        { title: 'Old Status', key: 'oldStatus', sortable: false },
        { title: 'New Status', key: 'newStatus', sortable: false }
      ]
)

const refreshRecords = async () => {
  pagination.page = 1
  await loadRecords()
}

const handleTableOptions = async ({ page, itemsPerPage }) => {
  pagination.page = Math.min(Math.max(1, page || 1), totalPages.value)
  pagination.pageSize = itemsPerPage || 10
  await loadRecords()
}

const myanmarDateTimeFormatter = new Intl.DateTimeFormat('en-US', {
  timeZone: 'Asia/Yangon',
  year: 'numeric',
  month: 'numeric',
  day: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
  second: '2-digit',
  hour12: true
})

const toUtcDate = (value) => {
  if (!value) return null
  const text = String(value)
  const hasTimezone = /(?:Z|[+-]\d{2}:?\d{2})$/i.test(text)
  return new Date(hasTimezone ? text : `${text}Z`)
}

const formatDate = (value) => {
  const date = toUtcDate(value)
  return date && !Number.isNaN(date.getTime()) ? myanmarDateTimeFormatter.format(date) : '-'
}

const roleClass = (roleId) => {
  const normalized = String(roleId || '').toLowerCase()
  if (normalized === 'admin') return 'role-admin'
  if (normalized === 'dispatcher') return 'role-dispatcher'
  if (normalized === 'mechanic') return 'role-mechanic'
  return 'role-driver'
}

const actionClass = (action) => {
  const normalized = String(action || '').toLowerCase()
  if (normalized === 'create') return 'role-driver'
  if (normalized === 'edit') return 'role-dispatcher'
  if (normalized === 'delete') return 'role-admin'
  return 'role-mechanic'
}

const statusClass = (status) => {
  const normalized = String(status || '').toLowerCase()
  if (normalized === 'active' || normalized === 'completed' || normalized === 'closed') return 'role-driver'
  if (normalized === 'pending' || normalized === 'maintenance') return 'role-mechanic'
  if (normalized === 'inactive' || normalized === 'cancelled') return 'role-admin'
  return 'role-dispatcher'
}

onMounted(loadRecords)
</script>

<style scoped src="./page_styles/AuditLogs.css"></style>
