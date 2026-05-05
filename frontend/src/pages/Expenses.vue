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

      <section class="toolbar">
        <div class="search-box">
          <v-icon icon="mdi-magnify" size="22" />
          <input v-model="filters.search" placeholder="Search expense, vehicle, trip, driver..." @input="loadRecords" />
        </div>
        <select v-model="filters.status" @change="loadRecords">
          <option value="">All Status</option>
          <option v-for="status in statusOptions" :key="status" :value="status">{{ status }}</option>
        </select>
        <input v-model="filters.dateFrom" type="date" @change="loadRecords" />
        <input v-model="filters.dateTo" type="date" @change="loadRecords" />
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
        <table>
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
              <td>{{ index + 1 }}</td>
              <td>{{ expense.expenseDate }}</td>
              <td>{{ expense.expenseType }}</td>
              <td>{{ expense.vehicleId || '-' }}</td>
              <td>{{ expense.tripNumber || '-' }}</td>
              <td>{{ expense.driverName || '-' }}</td>
              <td>{{ formatCurrency(expense.amount) }}</td>
              <td><span class="status-pill">{{ expense.status }}</span></td>
              <td>
                <div class="row-actions">
                  <button v-if="canEdit" type="button" @click="startEdit(expense)"><v-icon icon="mdi-pencil" size="18" /></button>
                  <button v-if="canDelete" type="button" class="danger" @click="removeRecord(expense.id)"><v-icon icon="mdi-delete-outline" size="18" /></button>
                </div>
              </td>
            </tr>
            <tr v-if="!records.length">
              <td colspan="9" class="empty-cell">No expense records found</td>
            </tr>
          </tbody>
        </table>
      </section>
    </main>
  </DashboardLayout>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import DashboardLayout from '../layouts/DashboardLayout.vue'
import { createExpense, deleteExpense, getExpenses, updateExpense } from '../services/expensesApi'
import { expenseTypesApi, statusesApi } from '../services/tripSetupApi'
import { canCreateModule, canDeleteModule, canEditModule } from '../utils/authSession'

const moduleKey = 'expenses'
const records = ref([])
const showForm = ref(false)
const editingId = ref(null)
const expenseTypeOptions = ref(['Fuel', 'Toll', 'Repair', 'Parking', 'Insurance', 'Tax'])
const statusOptions = ref(['Active', 'Pending', 'Approved', 'Paid'])
const filters = reactive({ search: '', status: '', dateFrom: '', dateTo: '' })
const form = reactive({ expenseDate: '', expenseType: '', vehicleId: '', tripNumber: '', driverName: '', amount: 0, status: 'Active', notes: '' })

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
  const result = await getExpenses({ ...filters, pageSize: 100 })
  records.value = result?.items || []
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
  if (editingId.value) await updateExpense(editingId.value, form)
  else await createExpense(form)
  showForm.value = false
  resetForm()
  await loadRecords()
}

const removeRecord = async (id) => {
  await deleteExpense(id)
  await loadRecords()
}

const formatCurrency = (value) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(Number(value || 0))

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
.search-box { display: flex; align-items: center; gap: 10px; }
input, select { width: 100%; min-height: 44px; border: 1px solid #dfe3ea; border-radius: 10px; padding: 0 12px; font: inherit; background: #fff; }
.record-form { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.form-actions { display: flex; justify-content: flex-end; gap: 10px; grid-column: 1 / -1; }
.primary-button, .ghost-button { min-height: 44px; border: 0; border-radius: 10px; padding: 0 18px; font-weight: 700; display: inline-flex; align-items: center; justify-content: center; gap: 8px; cursor: pointer; }
.primary-button { background: #2563eb; color: white; }
.ghost-button { background: #eef2f7; color: #334155; }
table { width: 100%; border-collapse: collapse; }
th, td { padding: 14px; text-align: left; border-bottom: 1px solid #e5e7eb; }
th { color: #475569; font-weight: 700; }
.status-pill { display: inline-flex; padding: 5px 12px; border-radius: 999px; background: #dcfce7; color: #15803d; font-weight: 700; }
.row-actions { display: flex; gap: 10px; }
.row-actions button { border: 0; background: transparent; color: #2563eb; cursor: pointer; }
.row-actions .danger { color: #dc2626; }
.empty-cell { text-align: center; color: #64748b; padding: 48px; }
@media (max-width: 900px) { .toolbar, .record-form { grid-template-columns: 1fr; } .records-header { align-items: stretch; flex-direction: column; } }
</style>
