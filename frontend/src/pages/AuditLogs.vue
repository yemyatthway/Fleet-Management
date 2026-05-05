<template>
  <DashboardLayout>
    <main class="records-page">
      <header class="records-header">
        <div>
          <h1>Audit Logs</h1>
          <p>Review create, edit, delete, and status-change activity across modules.</p>
        </div>
      </header>

      <section class="toolbar">
        <select v-model="mode" @change="loadRecords">
          <option value="audit">Audit Logs</option>
          <option value="status">Status History</option>
        </select>
        <input v-if="mode === 'audit'" v-model="filters.module" placeholder="Module key" @input="loadRecords" />
        <input v-if="mode === 'status'" v-model="filters.entityType" placeholder="Entity type" @input="loadRecords" />
        <input v-if="mode === 'status'" v-model="filters.entityId" placeholder="Entity ID" @input="loadRecords" />
      </section>

      <section class="table-card">
        <table v-if="mode === 'audit'">
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
              <td>{{ log.roleId }}</td>
              <td>{{ log.moduleKey }}</td>
              <td>{{ log.action }}</td>
              <td>{{ log.entityId }}</td>
              <td>{{ log.description }}</td>
            </tr>
            <tr v-if="!records.length">
              <td colspan="6" class="empty-cell">No audit logs found</td>
            </tr>
          </tbody>
        </table>

        <table v-else>
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
              <td>{{ history.roleId }}</td>
              <td>{{ history.entityType }}</td>
              <td>{{ history.entityId }}</td>
              <td>{{ history.oldStatus || '-' }}</td>
              <td>{{ history.newStatus }}</td>
            </tr>
            <tr v-if="!records.length">
              <td colspan="6" class="empty-cell">No status history found</td>
            </tr>
          </tbody>
        </table>
      </section>
    </main>
  </DashboardLayout>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import DashboardLayout from '../layouts/DashboardLayout.vue'
import { getAuditLogs, getStatusHistory } from '../services/auditApi'

const mode = ref('audit')
const records = ref([])
const filters = reactive({ module: '', entityType: '', entityId: '' })

const loadRecords = async () => {
  const result = mode.value === 'audit'
    ? await getAuditLogs({ module: filters.module, pageSize: 100 })
    : await getStatusHistory({ entityType: filters.entityType, entityId: filters.entityId, pageSize: 100 })
  records.value = result?.items || []
}

const formatDate = (value) => value ? new Date(value).toLocaleString() : '-'

onMounted(loadRecords)
</script>

<style scoped>
.records-page { padding: 28px 32px; display: grid; gap: 20px; }
.records-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
h1 { margin: 0; font-size: 28px; }
p { margin: 6px 0 0; color: #64748b; }
.toolbar, .table-card { background: #fff; border: 1px solid #dfe3ea; border-radius: 16px; padding: 18px; }
.toolbar { display: grid; grid-template-columns: 220px 1fr 1fr; gap: 14px; }
input, select { width: 100%; min-height: 44px; border: 1px solid #dfe3ea; border-radius: 10px; padding: 0 12px; font: inherit; background: #fff; }
table { width: 100%; border-collapse: collapse; }
th, td { padding: 14px; text-align: left; border-bottom: 1px solid #e5e7eb; }
th { color: #475569; font-weight: 700; }
.empty-cell { text-align: center; color: #64748b; padding: 48px; }
@media (max-width: 900px) { .toolbar { grid-template-columns: 1fr; } }
</style>
