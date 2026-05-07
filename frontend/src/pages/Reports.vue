<template>
  <DashboardLayout>
    <main class="records-page">
      <header class="records-header">
        <div>
          <h1>Reports</h1>
          <p>Filter fleet records and export vehicle, trip, maintenance, driver, and expense reports.</p>
        </div>
        <div class="header-actions">
          <button class="export-button excel-button" type="button" @click="exportExcel">
            <v-icon icon="mdi-file-excel-outline" size="20" />
            Excel
          </button>
          <button class="export-button pdf-button" type="button" @click="exportPdf">
            <v-icon icon="mdi-file-pdf-box" size="20" />
            PDF
          </button>
        </div>
      </header>

      <div v-if="pageError" class="page-error">{{ pageError }}</div>

      <section class="toolbar card-surface">
        <select v-model="reportType" @change="loadReport">
          <option v-for="option in reportOptions" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
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

      <section class="table-card card-surface" id="report-output">
        <v-data-table
          class="report-table"
          :headers="tableHeaders"
          :items="formattedRows"
          :items-per-page="10"
          :items-per-page-options="[10, 20, 30]"
          :mobile-breakpoint="0"
          :mobile="false"
          density="comfortable"
        >
          <template #no-data>
            <div class="empty-cell">No report records found</div>
          </template>
        </v-data-table>
      </section>
    </main>
  </DashboardLayout>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import DashboardLayout from '../layouts/DashboardLayout.vue'
import { getReport } from '../services/reportsApi'
import { statusesApi } from '../services/tripSetupApi'
import { exportRowsToPdf, exportRowsToXlsx } from '../utils/reportExport'
import { getCurrentUser } from '../utils/authSession'

const reportType = ref('vehicles')
const rows = ref([])
const pageError = ref('')
const statusOptions = ref(['Active', 'Pending', 'Completed', 'Maintenance', 'Inactive'])
const filters = reactive({ dateFrom: '', dateTo: '', status: '', vehicleId: '', driver: '' })
const currentUser = computed(() => getCurrentUser())
const isDispatcher = computed(() => String(currentUser.value?.roleId || currentUser.value?.role || '').toLowerCase() === 'dispatcher')

const allReportOptions = [
  { value: 'vehicles', label: 'Vehicle Report' },
  { value: 'trips', label: 'Trip Report' },
  { value: 'maintenance', label: 'Maintenance Report' },
  { value: 'drivers', label: 'Driver Report' },
  { value: 'expenses', label: 'Fuel/Expense Report' },
  { value: 'audit-logs', label: 'Audit Log Report' }
]

const reportOptions = computed(() =>
  isDispatcher.value
    ? allReportOptions.filter((option) => option.value === 'vehicles' || option.value === 'trips')
    : allReportOptions
)

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
  ],
  'audit-logs': [
    ['createdAt', 'Time'],
    ['roleId', 'Role'],
    ['moduleKey', 'Module'],
    ['action', 'Action'],
    ['entityId', 'Entity'],
    ['description', 'Description']
  ]
}

const reportNames = {
  vehicles: 'Vehicle',
  trips: 'Trip',
  maintenance: 'Maintenance',
  drivers: 'Driver',
  expenses: 'Fuel/Expense',
  'audit-logs': 'Audit Log'
}

const columns = computed(() => (columnMap[reportType.value] || []).map(([key, label]) => ({ key, label })))
const tableHeaders = computed(() => columns.value.map((column) => ({ title: column.label, key: column.key, sortable: false })))
const formattedRows = computed(() =>
  rows.value.map((row) =>
    columns.value.reduce((record, column) => {
      record[column.key] = formatCell(row[column.key], column.key)
      return record
    }, {})
  )
)
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
  pageError.value = ''
  try {
    rows.value = await getReport(reportType.value, filters)
  } catch (error) {
    rows.value = []
    pageError.value = error.message || 'Could not load report.'
  }
}

const formatCurrency = (value) => new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(Number(value || 0))
const formatCell = (value, key) => {
  if (key === 'amount') return formatCurrency(value)
  if (key === 'createdAt') return formatMyanmarDateTime(value)
  return value || '-'
}

const myanmarDateTimeFormatter = new Intl.DateTimeFormat('en-US', {
  timeZone: 'Asia/Yangon',
  year: 'numeric',
  month: 'short',
  day: 'numeric',
  hour: 'numeric',
  minute: '2-digit',
  hour12: true
})

const formatMyanmarDateTime = (value) => {
  if (!value) return '-'
  const text = String(value)
  const hasTimezone = /(?:Z|[+-]\d{2}:?\d{2})$/i.test(text)
  const date = new Date(hasTimezone ? text : `${text}Z`)
  return Number.isNaN(date.getTime()) ? '-' : myanmarDateTimeFormatter.format(date)
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
  subtitle: 'FleetManager operational report',
  columns: columns.value,
  rows: rows.value,
  formatCell,
  filters: {
    dateFrom: filters.dateFrom,
    dateTo: filters.dateTo,
    status: filters.status,
    vehicleId: filters.vehicleId,
    driver: filters.driver
  }
})

onMounted(async () => {
  await loadOptions()
  await loadReport()
})

watch(reportOptions, async (options) => {
  if (!options.some((option) => option.value === reportType.value)) {
    reportType.value = options[0]?.value || 'vehicles'
    await loadReport()
  }
})
</script>

<style scoped src="./page_styles/Reports.css"></style>
