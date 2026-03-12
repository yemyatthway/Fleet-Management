<template>
  <div class="maintenance-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Maintenance Tickets</h1>
        <p class="section-subtitle">Track vehicle issues and repair progress</p>
      </div>
      <button class="primary-button" type="button" @click="openCreate">
        <v-icon icon="mdi-wrench" size="18" />
        Create Ticket
      </button>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Tickets</p>
        <h3>{{ tickets.length }}</h3>
        <span class="stat-foot text-muted">All open and closed</span>
      </div>
      <div class="stat-card">
        <p>Pending</p>
        <h3 class="text-warning">{{ pendingCount }}</h3>
        <span class="stat-foot text-muted">Awaiting action</span>
      </div>
      <div class="stat-card">
        <p>Repairing</p>
        <h3 class="text-info">{{ repairingCount }}</h3>
        <span class="stat-foot text-muted">In progress</span>
      </div>
      <div class="stat-card">
        <p>Completed</p>
        <h3 class="text-success">{{ completedCount }}</h3>
        <span class="stat-foot text-muted">Resolved</span>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search by vehicle or issue..."
          />
        </div>

        <div class="toolbar-filter">
          <v-icon icon="mdi-filter-variant" />
          <select v-model="statusFilter">
            <option value="All">All Status</option>
            <option value="Pending">Pending</option>
            <option value="Repairing">Repairing</option>
            <option value="Completed">Completed</option>
          </select>
        </div>
      </div>

      <div class="toolbar-count text-muted">
        Showing {{ filteredTickets.length }} of {{ tickets.length }} tickets
      </div>
    </div>

    <div class="card-surface">
      <div class="table-wrap">
        <table class="table-base">
          <thead>
            <tr>
              <th>Vehicle</th>
              <th>Issue</th>
              <th>Reported Date</th>
              <th>Mechanic</th>
              <th>Status</th>
              <th class="align-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="ticket in filteredTickets" :key="ticket.id">
              <td>
                <div class="vehicle-cell">
                  <span class="vehicle-avatar">{{ initials(ticket.vehicle) }}</span>
                  <div>
                    <strong>{{ ticket.vehicle }}</strong>
                    <div class="text-muted vehicle-sub">{{ ticket.vehicleId }}</div>
                  </div>
                </div>
              </td>
              <td>
                <div class="issue-cell">
                  <span class="issue-title">{{ ticket.issue }}</span>
                  <span class="issue-note text-muted">{{ ticket.details }}</span>
                </div>
              </td>
              <td class="text-muted">{{ formatDate(ticket.reportedDate) }}</td>
              <td>
                <div class="mechanic-cell">
                  <span class="mechanic-avatar">{{ initials(ticket.mechanic) }}</span>
                  <span>{{ ticket.mechanic }}</span>
                </div>
              </td>
              <td>
                <span class="badge" :class="statusClass(ticket.status)">
                  {{ ticket.status }}
                </span>
              </td>
              <td class="align-right">
                <div class="inline-actions">
                  <button class="icon-button" type="button" @click="openEdit(ticket)">
                    <v-icon icon="mdi-pencil-outline" size="18" />
                  </button>
                  <button
                    class="icon-button"
                    :class="ticket.status === 'Completed' ? 'good' : 'warn'"
                    type="button"
                    @click="advanceStatus(ticket.id)"
                  >
                    <v-icon icon="mdi-progress-wrench" size="18" />
                  </button>
                  <button class="icon-button danger" type="button" @click="deleteTicket(ticket.id)">
                    <v-icon icon="mdi-trash-can-outline" size="18" />
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-if="filteredTickets.length === 0" class="empty-state">
        No tickets found matching your criteria
      </div>
    </div>

    <v-dialog v-model="formOpen" max-width="520">
      <v-card class="dialog-card">
        <div class="dialog-header">
          <h2>{{ formMode === 'create' ? 'Create Ticket' : 'Edit Ticket' }}</h2>
          <button class="icon-button" type="button" @click="formOpen = false">
            <v-icon icon="mdi-close" />
          </button>
        </div>
        <form class="dialog-body" @submit.prevent="submitForm">
          <div class="field">
            <label>Vehicle</label>
            <input v-model="form.vehicle" type="text" placeholder="Box Truck" required />
          </div>
          <div class="field">
            <label>Vehicle ID</label>
            <input v-model="form.vehicleId" type="text" placeholder="VH-2048" required />
          </div>
          <div class="field">
            <label>Issue</label>
            <input v-model="form.issue" type="text" placeholder="Brake Inspection" required />
          </div>
          <div class="field">
            <label>Details</label>
            <input v-model="form.details" type="text" placeholder="Short description" required />
          </div>
          <div class="field">
            <label>Reported Date</label>
            <input v-model="form.reportedDate" type="date" required />
          </div>
          <div class="field">
            <label>Mechanic</label>
            <input v-model="form.mechanic" type="text" placeholder="Mechanic name" required />
          </div>
          <div class="field">
            <label>Status</label>
            <select v-model="form.status" required>
              <option value="Pending">Pending</option>
              <option value="Repairing">Repairing</option>
              <option value="Completed">Completed</option>
            </select>
          </div>

          <div class="dialog-actions">
            <button class="ghost" type="button" @click="formOpen = false">Cancel</button>
            <button class="primary" type="submit">
              {{ formMode === 'create' ? 'Create Ticket' : 'Save Changes' }}
            </button>
          </div>
        </form>
      </v-card>
    </v-dialog>

    <ConfirmDialog
      :open="confirmOpen"
      :title="confirmTitle"
      :message="confirmMessage"
      :confirm-text="confirmButton"
      :tone="confirmTone"
      @confirm="runConfirm"
      @cancel="confirmOpen = false"
    />
  </div>
</template>

<script setup>
import { computed, reactive, ref } from 'vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'

const tickets = ref([
  {
    id: 'MT-2031',
    vehicle: 'Box Truck',
    vehicleId: 'VH-2048',
    issue: 'Brake Inspection',
    details: 'Scheduled brake pad replacement',
    reportedDate: '2026-02-28',
    mechanic: 'Daniel Harris',
    status: 'Pending'
  },
  {
    id: 'MT-2032',
    vehicle: 'Cargo Van',
    vehicleId: 'VH-3054',
    issue: 'Engine Overheat',
    details: 'Cooling system diagnostics',
    reportedDate: '2026-03-02',
    mechanic: 'Maya Lopez',
    status: 'Repairing'
  },
  {
    id: 'MT-2033',
    vehicle: 'Reefer Truck',
    vehicleId: 'VH-1987',
    issue: 'Refrigeration Unit',
    details: 'Temperature fluctuation detected',
    reportedDate: '2026-02-22',
    mechanic: 'Alex Chen',
    status: 'Completed'
  },
  {
    id: 'MT-2034',
    vehicle: 'Flatbed',
    vehicleId: 'VH-4129',
    issue: 'Hydraulic Leak',
    details: 'Seal replacement required',
    reportedDate: '2026-03-05',
    mechanic: 'Isabella Park',
    status: 'Repairing'
  },
  {
    id: 'MT-2035',
    vehicle: 'Delivery Van',
    vehicleId: 'VH-2661',
    issue: 'Tire Alignment',
    details: 'Front axle alignment',
    reportedDate: '2026-03-01',
    mechanic: 'Marcus Reed',
    status: 'Pending'
  }
])

const searchQuery = ref('')
const statusFilter = ref('All')
const formOpen = ref(false)
const formMode = ref('create')
const editingId = ref(null)
const confirmOpen = ref(false)
const confirmTitle = ref('Are you sure?')
const confirmMessage = ref('')
const confirmButton = ref('Confirm')
const confirmTone = ref('danger')
const pendingAction = ref(() => {})

const form = reactive({
  vehicle: '',
  vehicleId: '',
  issue: '',
  details: '',
  reportedDate: '',
  mechanic: '',
  status: 'Pending'
})

const filteredTickets = computed(() => {
  const query = searchQuery.value.toLowerCase()
  return tickets.value.filter((ticket) => {
    const matchesSearch =
      ticket.vehicle.toLowerCase().includes(query) ||
      ticket.issue.toLowerCase().includes(query)
    const matchesStatus = statusFilter.value === 'All' || ticket.status === statusFilter.value
    return matchesSearch && matchesStatus
  })
})

const pendingCount = computed(() => tickets.value.filter((t) => t.status === 'Pending').length)
const repairingCount = computed(() => tickets.value.filter((t) => t.status === 'Repairing').length)
const completedCount = computed(() => tickets.value.filter((t) => t.status === 'Completed').length)

const statusClass = (status) => {
  if (status === 'Completed') return 'success'
  if (status === 'Repairing') return 'info'
  return 'warning'
}

const initials = (name) => name.split(' ').map((part) => part[0]).join('')

const formatDate = (value) =>
  new Date(value).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })

const resetForm = () => {
  form.vehicle = ''
  form.vehicleId = ''
  form.issue = ''
  form.details = ''
  form.reportedDate = ''
  form.mechanic = ''
  form.status = 'Pending'
}

const openCreate = () => {
  formMode.value = 'create'
  editingId.value = null
  resetForm()
  formOpen.value = true
}

const openEdit = (ticket) => {
  formMode.value = 'edit'
  editingId.value = ticket.id
  form.vehicle = ticket.vehicle
  form.vehicleId = ticket.vehicleId
  form.issue = ticket.issue
  form.details = ticket.details
  form.reportedDate = ticket.reportedDate
  form.mechanic = ticket.mechanic
  form.status = ticket.status
  formOpen.value = true
}

const submitForm = () => {
  if (formMode.value === 'create') {
    const nextId = `MT-${2030 + tickets.value.length + 1}`
    tickets.value.unshift({
      id: nextId,
      vehicle: form.vehicle,
      vehicleId: form.vehicleId,
      issue: form.issue,
      details: form.details,
      reportedDate: form.reportedDate,
      mechanic: form.mechanic,
      status: form.status
    })
  } else if (editingId.value) {
    tickets.value = tickets.value.map((ticket) =>
      ticket.id === editingId.value
        ? {
            ...ticket,
            vehicle: form.vehicle,
            vehicleId: form.vehicleId,
            issue: form.issue,
            details: form.details,
            reportedDate: form.reportedDate,
            mechanic: form.mechanic,
            status: form.status
          }
        : ticket
    )
  }
  formOpen.value = false
}

const openConfirm = ({ title, message, confirmText, tone, action }) => {
  confirmTitle.value = title
  confirmMessage.value = message
  confirmButton.value = confirmText
  confirmTone.value = tone
  pendingAction.value = action
  confirmOpen.value = true
}

const runConfirm = () => {
  pendingAction.value()
  confirmOpen.value = false
}

const advanceStatus = (id) => {
  const ticket = tickets.value.find((item) => item.id === id)
  if (!ticket) return
  const nextStatus =
    ticket.status === 'Pending'
      ? 'Repairing'
      : ticket.status === 'Repairing'
      ? 'Completed'
      : 'Pending'

  openConfirm({
    title: 'Update Status?',
    message: `Move ${ticket.id} to ${nextStatus.toLowerCase()} status?`,
    confirmText: nextStatus,
    tone: 'warning',
    action: () => {
      tickets.value = tickets.value.map((item) =>
        item.id === id ? { ...item, status: nextStatus } : item
      )
    }
  })
}

const deleteTicket = (id) => {
  const ticket = tickets.value.find((item) => item.id === id)
  if (!ticket) return
  openConfirm({
    title: 'Delete Ticket?',
    message: `This will permanently remove ${ticket.id}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: () => {
      tickets.value = tickets.value.filter((item) => item.id !== id)
    }
  })
}
</script>

<style scoped>
.maintenance-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 16px;
}

.stat-card {
  padding: 18px;
  border-radius: 16px;
  border: 1px solid var(--fleet-border);
  background: #fff;
}

.stat-card p {
  margin: 0;
  font-size: 13px;
  color: var(--fleet-muted);
}

.stat-card h3 {
  margin: 8px 0 4px;
  font-size: 24px;
}

.stat-foot {
  font-size: 12px;
}

.text-success {
  color: var(--fleet-success);
}

.text-info {
  color: var(--fleet-primary);
}

.text-warning {
  color: var(--fleet-warning);
}

.toolbar {
  padding: 18px;
}

.toolbar-row {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
}

.toolbar-search,
.toolbar-filter {
  display: flex;
  align-items: center;
  gap: 8px;
  border: 1px solid var(--fleet-border);
  border-radius: 12px;
  padding: 10px 12px;
  background: #fff;
  min-width: 220px;
}

.toolbar-search input,
.toolbar-filter select {
  border: none;
  outline: none;
  background: transparent;
  font-size: 14px;
}

.toolbar-filter select {
  appearance: none;
}

.primary-button {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border: none;
  border-radius: 12px;
  padding: 10px 16px;
  background: var(--fleet-primary);
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}

.primary-button:hover {
  background: var(--fleet-primary-dark);
}

.table-wrap {
  overflow-x: auto;
}

.align-right {
  text-align: right;
}

.vehicle-cell,
.mechanic-cell {
  display: inline-flex;
  align-items: center;
  gap: 10px;
}

.vehicle-avatar,
.mechanic-avatar {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-weight: 700;
  color: #fff;
  background: linear-gradient(135deg, #2563eb, #1e40af);
  font-size: 12px;
}

.vehicle-sub {
  font-size: 12px;
  margin-top: 2px;
}

.issue-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.issue-title {
  font-weight: 600;
}

.issue-note {
  font-size: 12px;
}

.empty-state {
  text-align: center;
  padding: 32px;
  color: var(--fleet-muted);
}

.dialog-card {
  border-radius: 18px;
  padding: 0;
}

.dialog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px;
  border-bottom: 1px solid var(--fleet-border);
}

.dialog-header h2 {
  margin: 0;
  font-size: 18px;
}

.dialog-body {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 20px 24px 24px;
}

.field label {
  display: block;
  font-size: 12px;
  font-weight: 600;
  color: #334155;
  margin-bottom: 6px;
}

.field input,
.field select {
  width: 100%;
  padding: 10px 12px;
  border-radius: 10px;
  border: 1px solid var(--fleet-border);
  font-size: 14px;
}

.dialog-actions {
  display: flex;
  gap: 12px;
  margin-top: 6px;
}

.dialog-actions button {
  flex: 1;
  border-radius: 10px;
  padding: 10px 12px;
  font-weight: 600;
  cursor: pointer;
  border: none;
}

.dialog-actions .ghost {
  background: #f8fafc;
  border: 1px solid var(--fleet-border);
  color: #334155;
}

.dialog-actions .primary {
  background: var(--fleet-primary);
  color: #fff;
}

.dialog-actions .primary:hover {
  background: var(--fleet-primary-dark);
}

.icon-button {
  border: none;
  background: transparent;
  width: 34px;
  height: 34px;
  border-radius: 10px;
  cursor: pointer;
  color: #2563eb;
}

.icon-button:hover {
  background: #eff6ff;
}

.icon-button.danger {
  color: #dc2626;
}

.icon-button.danger:hover {
  background: #fee2e2;
}

.icon-button.warn {
  color: #ea580c;
}

.icon-button.warn:hover {
  background: #ffedd5;
}

.icon-button.good {
  color: #16a34a;
}

.icon-button.good:hover {
  background: #dcfce7;
}
</style>
