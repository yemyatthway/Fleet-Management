<template>
  <DashboardLayout>
    <main class="records-page">
      <header class="records-header">
        <div>
          <h1>Expenses</h1>
          <p>Track fuel, repair, toll, parking, insurance, tax, and trip costs.</p>
        </div>
        <button v-if="canCreate" class="primary-button" type="button" @click="startCreate">
          <v-icon icon="mdi-plus" size="20" />
          Add Expense
        </button>
      </header>

      <PageMessage
        :tone="pageMessage.tone"
        :title="pageMessage.title"
        :message="pageMessage.message"
        @close="clearPageMessage"
      />

      <div v-if="pageError" class="page-error">{{ pageError }}</div>

      <section class="toolbar">
        <div class="search-box">
          <v-icon icon="mdi-magnify" size="22" />
          <input v-model="filters.search" placeholder="Search expense, vehicle, trip, driver..." @input="refreshRecords" />
        </div>
        <select v-model="filters.status" @change="refreshRecords">
          <option value="">All Status</option>
          <option v-for="status in statusOptions" :key="status" :value="status">{{ status }}</option>
        </select>
        <input v-model="filters.dateFrom" type="date" @change="refreshRecords" />
        <input v-model="filters.dateTo" type="date" @change="refreshRecords" />
      </section>

      <form v-if="showForm" class="record-form" @submit.prevent="saveRecord">
        <input v-model="form.expenseDate" type="date" required />
        <select v-model="form.expenseType" required>
          <option value="" disabled>Expense type</option>
          <option v-for="type in expenseTypeOptions" :key="type" :value="type">{{ type }}</option>
        </select>
        <input v-model="form.vehicleId" placeholder="Vehicle/ID" />
        <input v-model="form.tripNumber" placeholder="Trip number" />
        <input v-model="form.driverName" placeholder="Driver" />
        <input v-model.number="form.amount" min="0" step="0.01" type="number" placeholder="Amount" required />
        <select v-model="form.status" required>
          <option v-for="status in statusOptions" :key="status" :value="status">{{ status }}</option>
        </select>
        <input v-model="form.notes" placeholder="Notes" />
        <div class="form-actions">
          <button class="ghost-button" type="button" @click="cancelForm">Cancel</button>
          <button class="primary-button" type="submit">{{ editingId ? 'Save Expense' : 'Create Expense' }}</button>
        </div>
      </form>

      <section class="table-card">
        <div class="table-wrap">
        <table class="record-table">
          <thead>
            <tr>
              <th>No.</th>
              <th>Date</th>
              <th>Type</th>
              <th>Vehicle/ID</th>
              <th>Trip</th>
              <th>Driver</th>
              <th>Amount</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(expense, index) in records" :key="expense.id">
              <td>{{ pageStart + index }}</td>
              <td>{{ expense.expenseDate }}</td>
              <td>{{ expense.expenseType }}</td>
              <td>{{ expense.vehicleId || '-' }}</td>
              <td>{{ expense.tripNumber || '-' }}</td>
              <td>{{ expense.driverName || '-' }}</td>
              <td>{{ formatCurrency(expense.amount) }}</td>
              <td><span class="role-badge" :class="statusClass(expense.status)">{{ expense.status }}</span></td>
              <td>
                <div class="inline-actions">
                  <button v-if="canEdit" type="button" class="icon-button" @click="startEdit(expense)" aria-label="Edit expense"><v-icon icon="mdi-pencil" size="18" /></button>
                  <button v-if="canDelete" type="button" class="icon-button danger" @click="removeRecord(expense.id)" aria-label="Delete expense"><v-icon icon="mdi-delete-outline" size="18" /></button>
                </div>
              </td>
            </tr>
            <tr v-if="!records.length">
              <td colspan="9" class="empty-cell">No expense records found</td>
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
import PageMessage from '../components/common/PageMessage.vue'
import DashboardLayout from '../layouts/DashboardLayout.vue'
import { usePageMessage } from '../composables/usePageMessage'
import { createExpense, deleteExpense, getExpenses, updateExpense } from '../services/expensesApi'
import { expenseTypesApi, statusesApi } from '../services/tripSetupApi'
import { canCreateModule, canDeleteModule, canEditModule } from '../utils/authSession'

const moduleKey = 'expenses'
const records = ref([])
const totalRecords = ref(0)
const pageError = ref('')
const showForm = ref(false)
const editingId = ref(null)
const expenseTypeOptions = ref(['Fuel', 'Toll', 'Repair', 'Parking', 'Insurance', 'Tax'])
const statusOptions = ref(['Active', 'Pending', 'Approved', 'Paid'])
const filters = reactive({ search: '', status: '', dateFrom: '', dateTo: '' })
const pagination = reactive({ page: 1, pageSize: 10 })
const form = reactive({ expenseDate: '', expenseType: '', vehicleId: '', tripNumber: '', driverName: '', amount: 0, status: 'Active', notes: '' })
const { pageMessage, clearPageMessage, showPageMessage } = usePageMessage(4000)

const canCreate = computed(() => canCreateModule(moduleKey))
const canEdit = computed(() => canEditModule(moduleKey))
const canDelete = computed(() => canDeleteModule(moduleKey))

const loadOptions = async () => {
  try {
    const [types, statuses] = await Promise.all([expenseTypesApi.options(), statusesApi.options()])
    if (types?.length) expenseTypeOptions.value = types
    if (statuses?.length) statusOptions.value = statuses
  } catch (error) {
    console.error(error)
  }
}

const loadRecords = async () => {
  pageError.value = ''
  try {
    const result = await getExpenses({ ...filters, page: pagination.page, pageSize: pagination.pageSize })
    records.value = result?.items || []
    totalRecords.value = result?.total || 0

    const maxPage = Math.max(1, Math.ceil(totalRecords.value / pagination.pageSize))
    if (pagination.page > maxPage) {
      pagination.page = maxPage
      const retry = await getExpenses({ ...filters, page: pagination.page, pageSize: pagination.pageSize })
      records.value = retry?.items || []
      totalRecords.value = retry?.total || 0
    }
  } catch (error) {
    records.value = []
    totalRecords.value = 0
    pageError.value = error.message || 'Could not load expenses.'
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

const resetForm = () => {
  Object.assign(form, { expenseDate: '', expenseType: expenseTypeOptions.value[0] || '', vehicleId: '', tripNumber: '', driverName: '', amount: 0, status: statusOptions.value[0] || 'Active', notes: '' })
  editingId.value = null
}

const startCreate = () => {
  resetForm()
  showForm.value = true
}

const startEdit = (expense) => {
  Object.assign(form, { ...expense, notes: expense.notes || '', vehicleId: expense.vehicleId || '', tripNumber: expense.tripNumber || '', driverName: expense.driverName || '' })
  editingId.value = expense.id
  showForm.value = true
}

const cancelForm = () => {
  showForm.value = false
  resetForm()
}

const saveRecord = async () => {
  const isEdit = Boolean(editingId.value)
  pageError.value = ''
  try {
    if (isEdit) await updateExpense(editingId.value, form)
    else await createExpense(form)
    showForm.value = false
    resetForm()
    await loadRecords()
    showPageMessage({
      tone: 'success',
      title: isEdit ? 'Expense updated' : 'Expense created',
      message: isEdit ? 'Expense record was updated successfully.' : 'Expense record was created successfully.'
    })
  } catch (error) {
    pageError.value = error.message || 'Could not save expense.'
    showPageMessage({
      tone: 'error',
      title: 'Expense was not saved',
      message: pageError.value
    })
  }
}

const removeRecord = async (id) => {
  pageError.value = ''
  try {
    await deleteExpense(id)
    await loadRecords()
    showPageMessage({
      tone: 'success',
      title: 'Expense deleted',
      message: 'Expense record was deleted successfully.'
    })
  } catch (error) {
    pageError.value = error.message || 'Could not delete expense.'
    showPageMessage({
      tone: 'error',
      title: 'Expense was not deleted',
      message: pageError.value
    })
  }
}

const formatCurrency = (value) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(Number(value || 0))
const statusClass = (status) => {
  const normalized = String(status || '').toLowerCase()
  if (normalized === 'active' || normalized === 'approved' || normalized === 'paid') return 'role-driver'
  if (normalized === 'pending') return 'role-mechanic'
  if (normalized === 'rejected' || normalized === 'cancelled') return 'role-admin'
  return 'role-dispatcher'
}

onMounted(async () => {
  await loadOptions()
  resetForm()
  await loadRecords()
})
</script>

<style scoped>
.records-page { padding: 28px 32px; display: grid; gap: 20px; }
.records-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
h1 { margin: 0; font-size: 28px; }
p { margin: 6px 0 0; color: #64748b; }
.toolbar, .record-form, .table-card { background: #fff; border: 1px solid #dfe3ea; border-radius: 16px; padding: 18px; }
.toolbar { display: grid; grid-template-columns: minmax(260px, 1fr) 180px 160px 160px; gap: 14px; }
.search-box { display: flex; align-items: center; gap: 10px; min-height: 44px; height: 44px; max-height: 44px; }
input, select { width: 100%; min-height: 44px; height: 44px; max-height: 44px; border: 1px solid #dfe3ea; border-radius: 10px; padding: 0 12px; font: inherit; line-height: 1.2; background: #fff; box-sizing: border-box; }
.record-form { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.form-actions { display: flex; justify-content: flex-end; gap: 10px; grid-column: 1 / -1; }
.primary-button, .ghost-button { min-height: 44px; border: 0; border-radius: 10px; padding: 0 18px; font-weight: 700; display: inline-flex; align-items: center; justify-content: center; gap: 8px; cursor: pointer; }
.primary-button { background: #2563eb; color: white; }
.ghost-button { background: #eef2f7; color: #334155; }
.table-card { padding: 18px; overflow: hidden; }
.table-wrap { overflow-x: auto; }
.record-table { width: 100%; min-width: 980px; border-collapse: separate; border-spacing: 0; }
.record-table th, .record-table td { padding: 14px 16px; text-align: left; }
.record-table thead th { background: #f8fafc; color: #475569; font-size: 13px; letter-spacing: 0.02em; text-transform: uppercase; font-weight: 700; }
.record-table tbody td { background: #fff; }
.record-table tbody tr { box-shadow: 0 1px 2px rgba(15, 23, 42, 0.06); }
.record-table tbody tr td { border-bottom: 10px solid transparent; }
.record-table tbody tr:last-child td { border-bottom: 0; }
.record-table tbody tr:nth-child(even) td { background: #f8fafc; }
.record-table thead th:first-child, .record-table tbody td:first-child { border-radius: 12px 0 0 12px; }
.record-table thead th:last-child, .record-table tbody td:last-child { border-radius: 0 12px 12px 0; }
.record-table th:last-child, .record-table td:last-child { text-align: right; }
.role-badge { display: inline-flex; width: fit-content; padding: 4px 10px; border-radius: 999px; font-weight: 600; font-size: 12px; }
.role-admin { background: #ede9fe; color: #6d28d9; }
.role-dispatcher { background: #dbeafe; color: #1d4ed8; }
.role-driver { background: #dcfce7; color: #15803d; }
.role-mechanic { background: #ffedd5; color: #c2410c; }
.inline-actions { display: flex; justify-content: flex-end; gap: 6px; }
.icon-button { border: none; background: transparent; width: 34px; height: 34px; border-radius: 10px; cursor: pointer; color: #2563eb; }
.icon-button:hover { background: #eff6ff; }
.icon-button.danger { color: #dc2626; }
.icon-button.danger:hover { background: #fee2e2; }
.empty-cell { text-align: center !important; color: #64748b; padding: 48px !important; background: #fff !important; border-radius: 12px !important; }
.page-error { padding: 12px 14px; border: 1px solid #fecaca; border-radius: 12px; background: #fef2f2; color: #b91c1c; }
.table-footer { display: flex; align-items: center; justify-content: flex-end; gap: 14px; flex-wrap: wrap; padding-top: 14px; border-top: 1px solid #e5e7eb; }
.page-size { display: inline-flex; align-items: center; gap: 8px; color: #64748b; font-size: 13px; font-weight: 600; }
.page-size select { width: 86px; min-height: 36px; height: 36px; }
.pager-info { color: #64748b; font-size: 13px; font-weight: 600; }
.pager-actions { display: inline-flex; gap: 6px; }
.pager-button { width: 34px; height: 34px; display: inline-flex; align-items: center; justify-content: center; border: 1px solid #dfe3ea; border-radius: 10px; background: #fff; color: #334155; cursor: pointer; }
.pager-button:disabled { opacity: 0.5; cursor: not-allowed; }
@media (max-width: 900px) { .toolbar, .record-form { grid-template-columns: 1fr; } .records-header { align-items: stretch; flex-direction: column; } }
</style>
