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
        <table v-if="mode === 'audit'" class="record-table audit-table">
          <thead>
            <tr>
              <th>Time</th>
              <th>Role</th>
              <th>Module</th>
              <th>Action</th>
              <th>Entity</th>
              <th>Description</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="log in records" :key="log.id">
              <td>{{ formatDate(log.createdAt) }}</td>
              <td><span class="role-badge" :class="roleClass(log.roleId)">{{ log.roleId }}</span></td>
              <td><span class="module-key">{{ log.moduleKey }}</span></td>
              <td><span class="role-badge" :class="actionClass(log.action)">{{ log.action }}</span></td>
              <td>{{ log.entityId }}</td>
              <td>{{ log.description }}</td>
            </tr>
            <tr v-if="!records.length">
              <td colspan="6" class="empty-cell">No audit logs found</td>
            </tr>
          </tbody>
        </table>

        <table v-else class="record-table status-table">
          <thead>
            <tr>
              <th>Time</th>
              <th>Role</th>
              <th>Entity Type</th>
              <th>Entity ID</th>
              <th>Old Status</th>
              <th>New Status</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="history in records" :key="history.id">
              <td>{{ formatDate(history.createdAt) }}</td>
              <td><span class="role-badge" :class="roleClass(history.roleId)">{{ history.roleId }}</span></td>
              <td>{{ history.entityType }}</td>
              <td>{{ history.entityId }}</td>
              <td>{{ history.oldStatus || '-' }}</td>
              <td><span class="role-badge" :class="statusClass(history.newStatus)">{{ history.newStatus }}</span></td>
            </tr>
            <tr v-if="!records.length">
              <td colspan="6" class="empty-cell">No status history found</td>
            </tr>
          </tbody>
        </table>
        </div>
        <div class="table-footer">
          <label class="page-size">
            Items per page:
            <select v-model.number="pagination.pageSize" @change="refreshRecords">
              <option :value="10">10</option>
              <option :value="20">20</option>
              <option :value="30">30</option>
            </select>
          </label>
          <span class="pager-info">{{ pageStart }}-{{ pageEnd }} of {{ totalRecords }}</span>
          <div class="pager-actions">
            <button class="pager-button" type="button" :disabled="pagination.page === 1" @click="goToPage(1)">
              <v-icon icon="mdi-page-first" size="18" />
            </button>
            <button class="pager-button" type="button" :disabled="pagination.page === 1" @click="goToPage(pagination.page - 1)">
              <v-icon icon="mdi-chevron-left" size="18" />
            </button>
            <button class="pager-button" type="button" :disabled="pagination.page >= totalPages" @click="goToPage(pagination.page + 1)">
              <v-icon icon="mdi-chevron-right" size="18" />
            </button>
            <button class="pager-button" type="button" :disabled="pagination.page >= totalPages" @click="goToPage(totalPages)">
              <v-icon icon="mdi-page-last" size="18" />
            </button>
          </div>
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
const pageError = ref('')
const filters = reactive({ module: '', entityType: '', entityId: '' })
const pagination = reactive({ page: 1, pageSize: 10 })

const loadRecords = async () => {
  pageError.value = ''
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
  }
}

const totalPages = computed(() => Math.max(1, Math.ceil(totalRecords.value / pagination.pageSize)))
const pageStart = computed(() => totalRecords.value ? (pagination.page - 1) * pagination.pageSize + 1 : 0)
const pageEnd = computed(() => Math.min(totalRecords.value, pagination.page * pagination.pageSize))

const refreshRecords = async () => {
  pagination.page = 1
  await loadRecords()
}

const goToPage = async (page) => {
  pagination.page = Math.min(Math.max(1, page), totalPages.value)
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

<style scoped>
.records-page { padding: 28px 32px; display: grid; gap: 20px; }
.records-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
h1 { margin: 0; font-size: 28px; }
p { margin: 6px 0 0; color: #64748b; }
.toolbar, .table-card { background: #fff; border: 1px solid #dfe3ea; border-radius: 16px; padding: 18px; }
.toolbar { display: grid; grid-template-columns: 220px 1fr 1fr; gap: 14px; }
input, select { width: 100%; min-height: 44px; height: 44px; max-height: 44px; border: 1px solid #dfe3ea; border-radius: 10px; padding: 0 12px; font: inherit; line-height: 1.2; background: #fff; box-sizing: border-box; }
.table-card { overflow: hidden; }
.table-wrap { overflow-x: auto; }
.record-table { width: 100%; min-width: 1040px; border-collapse: separate; border-spacing: 0; }
.record-table th, .record-table td { padding: 14px 16px; text-align: left; vertical-align: middle; }
.record-table thead th { background: #f8fafc; color: #475569; font-size: 13px; letter-spacing: 0.02em; text-transform: uppercase; font-weight: 700; }
.record-table tbody td { background: #fff; }
.record-table tbody tr { box-shadow: 0 1px 2px rgba(15, 23, 42, 0.06); }
.record-table tbody tr td { border-bottom: 10px solid transparent; }
.record-table tbody tr:last-child td { border-bottom: 0; }
.record-table tbody tr:nth-child(even) td { background: #f8fafc; }
.record-table thead th:first-child, .record-table tbody td:first-child { border-radius: 12px 0 0 12px; }
.record-table thead th:last-child, .record-table tbody td:last-child { border-radius: 0 12px 12px 0; }
.audit-table th:first-child, .audit-table td:first-child { width: 210px; }
.audit-table th:nth-child(2), .audit-table td:nth-child(2) { width: 130px; }
.audit-table th:nth-child(3), .audit-table td:nth-child(3) { width: 180px; }
.audit-table th:nth-child(4), .audit-table td:nth-child(4) { width: 120px; }
.audit-table th:nth-child(5), .audit-table td:nth-child(5) { width: 140px; }
.status-table th:first-child, .status-table td:first-child { width: 210px; }
.role-badge { display: inline-flex; width: fit-content; padding: 4px 10px; border-radius: 999px; font-weight: 600; font-size: 12px; }
.role-admin { background: #ede9fe; color: #6d28d9; }
.role-dispatcher { background: #dbeafe; color: #1d4ed8; }
.role-driver { background: #dcfce7; color: #15803d; }
.role-mechanic { background: #ffedd5; color: #c2410c; }
.module-key { color: #475569; font-weight: 600; }
.page-error { padding: 12px 14px; border: 1px solid #fecaca; border-radius: 12px; background: #fef2f2; color: #b91c1c; }
.empty-cell { text-align: center !important; color: #64748b; padding: 48px !important; background: #fff !important; border-radius: 12px !important; }
.table-footer { display: flex; align-items: center; justify-content: flex-end; gap: 14px; flex-wrap: wrap; padding-top: 14px; border-top: 1px solid #e5e7eb; }
.page-size { display: inline-flex; align-items: center; gap: 8px; color: #64748b; font-size: 13px; font-weight: 600; }
.page-size select { width: 86px; min-height: 36px; height: 36px; }
.pager-info { color: #64748b; font-size: 13px; font-weight: 600; }
.pager-actions { display: inline-flex; gap: 6px; }
.pager-button { width: 34px; height: 34px; display: inline-flex; align-items: center; justify-content: center; border: 1px solid #dfe3ea; border-radius: 10px; background: #fff; color: #334155; cursor: pointer; }
.pager-button:disabled { opacity: 0.5; cursor: not-allowed; }
@media (max-width: 900px) { .toolbar { grid-template-columns: 1fr; } }
</style>
