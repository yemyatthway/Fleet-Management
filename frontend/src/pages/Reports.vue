<template>
  <DashboardLayout>
    <main class="records-page">
      <header class="records-header">
        <div>
          <h1>Reports</h1>
          <p>Filter fleet records and export vehicle, trip, maintenance, driver, and expense reports.</p>
        </div>
        <div class="header-actions">
          <button class="ghost-button" type="button" @click="exportExcel">
            <v-icon icon="mdi-file-excel-outline" size="20" />
            Excel
          </button>
          <button class="primary-button" type="button" @click="exportPdf">
            <v-icon icon="mdi-file-pdf-box" size="20" />
            PDF
          </button>
        </div>
      </header>

      <section class="toolbar">
        <select v-model="reportType" @change="loadReport">
          <option value="vehicles">Vehicle Report</option>
          <option value="trips">Trip Report</option>
          <option value="maintenance">Maintenance Report</option>
          <option value="drivers">Driver Report</option>
          <option value="expenses">Fuel/Expense Report</option>
        </select>
        <input v-model="filters.dateFrom" type="date" @change="loadReport" />
        <input v-model="filters.dateTo" type="date" @change="loadReport" />
        <input v-model="filters.vehicleId" placeholder="Vehicle/ID" @input="loadReport" />
        <input v-model="filters.driver" placeholder="Driver" @input="loadReport" />
        <select v-model="filters.status" @change="loadReport">
          <option value="">All Status</option>
          <option v-for="status in statusOptions" :key="status" :value="status">{{ status }}</option>
        </select>
      </section>

      <section class="summary-grid">
        <div class="summary-card">
          <span>Records</span>
          <strong>{{ rows.length }}</strong>
        </div>
        <div class="summary-card">
          <span>Report Type</span>
          <strong>{{ activeReportName }}</strong>
        </div>
        <div class="summary-card">
          <span>Total Amount</span>
          <strong>{{ reportType === 'expenses' ? formatCurrency(totalAmount) : '-' }}</strong>
        </div>
      </section>

      <section class="table-card" id="report-output">
        <table>
          <thead>
            <tr>
              <th v-for="column in columns" :key="column.key">{{ column.label }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(row, index) in rows" :key="row.id || index">
              <td v-for="column in columns" :key="column.key">{{ formatCell(row[column.key], column.key) }}</td>
            </tr>
            <tr v-if="!rows.length">
              <td :colspan="columns.length" class="empty-cell">No report records found</td>
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
import { getReport } from '../services/reportsApi'
import { statusesApi } from '../services/tripSetupApi'
import { exportRowsToPdf, exportRowsToXlsx } from '../utils/reportExport'

const reportType = ref('vehicles')
const rows = ref([])
const statusOptions = ref(['Active', 'Pending', 'Completed', 'Maintenance', 'Inactive'])
const filters = reactive({ dateFrom: '', dateTo: '', status: '', vehicleId: '', driver: '' })

const columnMap = {
  vehicles: [
    ['id', 'Vehicle/ID'],
    ['plate', 'Plate Number'],
    ['type', 'Type'],
    ['status', 'Status'],
    ['driver', 'Driver'],
    ['depot', 'Depot']
  ],
  trips: [
    ['tripNumber', 'Trip'],
    ['vehicleId', 'Vehicle/ID'],
    ['driverName', 'Driver'],
    ['status', 'Status'],
    ['pickupLocation', 'Pickup'],
    ['dropoffLocation', 'Dropoff']
  ],
  maintenance: [
    ['id', 'Ticket'],
    ['vehicleId', 'Vehicle/ID'],
    ['issue', 'Issue'],
    ['mechanic', 'Mechanic'],
    ['status', 'Status'],
    ['reportedDate', 'Reported']
  ],
  drivers: [
    ['employeeId', 'Employee ID'],
    ['name', 'Driver'],
    ['email', 'Email'],
    ['phone', 'Phone'],
    ['status', 'Status'],
    ['licenseExpiry', 'License Expiry']
  ],
  expenses: [
    ['expenseDate', 'Date'],
    ['expenseType', 'Type'],
    ['vehicleId', 'Vehicle/ID'],
    ['tripNumber', 'Trip'],
    ['driverName', 'Driver'],
    ['amount', 'Amount'],
    ['status', 'Status']
  ]
}

const reportNames = {
  vehicles: 'Vehicle',
  trips: 'Trip',
  maintenance: 'Maintenance',
  drivers: 'Driver',
  expenses: 'Fuel/Expense'
}

const columns = computed(() => (columnMap[reportType.value] || []).map(([key, label]) => ({ key, label })))
const activeReportName = computed(() => reportNames[reportType.value] || 'Report')
const totalAmount = computed(() => rows.value.reduce((sum, row) => sum + Number(row.amount || 0), 0))

const loadOptions = async () => {
  try {
    const statuses = await statusesApi.options()
    if (statuses?.length) statusOptions.value = statuses
  } catch (error) {
    console.error(error)
  }
}

const loadReport = async () => {
  rows.value = await getReport(reportType.value, filters)
}

const formatCurrency = (value) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(Number(value || 0))
const formatCell = (value, key) => {
  if (key === 'amount') return formatCurrency(value)
  return value || '-'
}

const exportExcel = () => exportRowsToXlsx({
  fileName: `${reportType.value}-report.xlsx`,
  sheetName: activeReportName.value,
  columns: columns.value,
  rows: rows.value,
  formatCell
})

const exportPdf = () => exportRowsToPdf({
  fileName: `${reportType.value}-report.pdf`,
  title: `${activeReportName.value} Report`,
  columns: columns.value,
  rows: rows.value,
  formatCell
})

onMounted(async () => {
  await loadOptions()
  await loadReport()
})
</script>

<style scoped>
.records-page { padding: 28px 32px; display: grid; gap: 20px; }
.records-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
h1 { margin: 0; font-size: 28px; }
p { margin: 6px 0 0; color: #64748b; }
.header-actions { display: flex; gap: 10px; }
.toolbar, .table-card, .summary-card { background: #fff; border: 1px solid #dfe3ea; border-radius: 16px; padding: 18px; }
.toolbar { display: grid; grid-template-columns: repeat(6, minmax(0, 1fr)); gap: 14px; }
.summary-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 16px; }
.summary-card span { display: block; color: #64748b; margin-bottom: 10px; }
.summary-card strong { font-size: 24px; }
input, select { width: 100%; min-height: 44px; height: 44px; max-height: 44px; border: 1px solid #dfe3ea; border-radius: 10px; padding: 0 12px; font: inherit; line-height: 1.2; background: #fff; box-sizing: border-box; }
.primary-button, .ghost-button { min-height: 44px; border: 0; border-radius: 10px; padding: 0 18px; font-weight: 700; display: inline-flex; align-items: center; justify-content: center; gap: 8px; cursor: pointer; }
.primary-button { background: #2563eb; color: white; }
.ghost-button { background: #eef2f7; color: #334155; }
table { width: 100%; border-collapse: collapse; }
th, td { padding: 14px; text-align: left; border-bottom: 1px solid #e5e7eb; }
th { color: #475569; font-weight: 700; }
.empty-cell { text-align: center; color: #64748b; padding: 48px; }
@media print {
  .records-header, .toolbar, .summary-grid { display: none; }
  .records-page { padding: 0; }
  .table-card { border: 0; }
}
@media (max-width: 1100px) { .toolbar, .summary-grid { grid-template-columns: 1fr 1fr; } }
@media (max-width: 700px) { .toolbar, .summary-grid { grid-template-columns: 1fr; } .records-header { align-items: stretch; flex-direction: column; } }
</style>
