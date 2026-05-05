<template>
  <div class="role-page incident-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Incident Management</h1>
        <p class="section-subtitle">Track accident and incident records across the fleet.</p>
      </div>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Incidents</p>
        <h3>{{ incidents.length }}</h3>
        <span class="stat-foot text-muted">Recorded cases</span>
      </div>
      <div class="stat-card">
        <p>Open</p>
        <h3 class="text-warning">{{ openCount }}</h3>
        <span class="stat-foot text-muted">Needs follow-up</span>
      </div>
      <div class="stat-card">
        <p>High Severity</p>
        <h3 class="text-danger">{{ highSeverityCount }}</h3>
        <span class="stat-foot text-muted">Priority review</span>
      </div>
      <div class="stat-card">
        <p>Closed</p>
        <h3 class="text-success">{{ closedCount }}</h3>
        <span class="stat-foot text-muted">Resolved cases</span>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search vehicle, driver, type, or notes..."
          />
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

        <div class="toolbar-actions">
          <div class="toolbar-filter">
            <v-icon icon="mdi-filter-variant" />
            <select v-model="statusFilter">
              <option value="All">All Status</option>
              <option v-for="status in incidentStatusFilterOptions" :key="status" :value="status">
                {{ status }}
              </option>
            </select>
          </div>

          <div class="toolbar-filter">
            <v-icon icon="mdi-alert-outline" />
            <select v-model="severityFilter">
              <option value="All">All Severity</option>
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </div>

          <button class="primary-button" type="button" @click="openIncident">
            <v-icon icon="mdi-alert-circle-outline" size="18" />
            Report Incident
          </button>
        </div>
      </div>

      <div class="toolbar-count text-muted">
        Showing {{ filteredIncidents.length }} of {{ incidents.length }} incidents
      </div>
    </div>

    <div class="card-surface table-card">
      <div class="table-wrap">
        <v-data-table
          class="table-base incident-table"
          :headers="incidentHeaders"
          :items="filteredIncidents"
          :items-per-page="10"
          :items-per-page-options="[10, 20, 30]"
          :mobile-breakpoint="0"
          :mobile="false"
          fixed-header
          height="520"
          density="comfortable"
        >
          <template #item.date="{ item }">
            <span class="text-muted">{{ formatDate(item.date) }}</span>
          </template>

          <template #item.vehicle="{ item }">
            <div class="vehicle-cell">
              <span class="vehicle-avatar">{{ initials(item.vehicleId) }}</span>
              <div>
                <strong>{{ item.vehicleId }}</strong>
                <div class="text-muted vehicle-sub">{{ item.driver }}</div>
              </div>
            </div>
          </template>

          <template #item.severity="{ item }">
            <span class="role-badge" :class="severityClass(item.severity)">
              {{ item.severity }}
            </span>
          </template>

          <template #item.status="{ item }">
            <span class="role-badge" :class="item.status === 'Open' ? 'role-mechanic' : 'role-driver'">
              {{ item.status }}
            </span>
          </template>

          <template #item.cost="{ item }">
            <span>{{ item.cost || '—' }}</span>
          </template>

          <template #item.actions="{ item }">
            <div class="inline-actions">
              <button class="icon-button tooltip" type="button" @click="openIncidentDetails(item)">
                <v-icon icon="mdi-eye-outline" size="18" />
                <span class="tooltip-text">View details</span>
              </button>
              <button class="icon-button tooltip" type="button" @click="openIncidentEdit(item)">
                <v-icon icon="mdi-pencil-outline" size="18" />
                <span class="tooltip-text">Edit incident</span>
              </button>
              <button class="icon-button danger tooltip" type="button" @click="deleteIncident(item.id)">
                <v-icon icon="mdi-trash-can-outline" size="18" />
                <span class="tooltip-text">Delete incident</span>
              </button>
            </div>
          </template>

          <template #no-data>
            <div class="empty-state">No incidents found matching your criteria</div>
          </template>
        </v-data-table>
      </div>
    </div>

    <v-dialog v-model="incidentOpen" max-width="720">
      <div class="card-surface form-card">
        <div class="form-header">
          <div class="form-title">{{ incidentMode === 'edit' ? 'Edit Incident' : 'Report Incident' }}</div>
          <button class="icon-button" type="button" @click="incidentOpen = false">
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>

        <div v-if="incidentError" class="form-error">{{ incidentError }}</div>

        <div class="form-grid">
          <div class="form-field">
            <label>Vehicle</label>
            <select v-model="incidentForm.vehicleId">
              <option disabled value="">Select vehicle</option>
              <option v-for="vehicle in vehicleOptions" :key="vehicle.id" :value="vehicle.id">
                {{ vehicle.id }} - {{ vehicle.model }}
              </option>
            </select>
          </div>
          <div class="form-field">
            <label>Driver</label>
            <input v-model="incidentForm.driver" type="text" placeholder="Driver name" />
          </div>
          <div class="form-field">
            <label>Date</label>
            <input v-model="incidentForm.date" type="date" />
          </div>
          <div class="form-field">
            <label>Type</label>
            <input v-model="incidentForm.type" type="text" placeholder="e.g., Collision" />
          </div>
          <div class="form-field">
            <label>Severity</label>
            <select v-model="incidentForm.severity">
              <option value="Low">Low</option>
              <option value="Medium">Medium</option>
              <option value="High">High</option>
            </select>
          </div>
          <div class="form-field">
            <label>Status</label>
            <select v-model="incidentForm.status">
              <option value="" disabled>Select status</option>
              <option v-for="status in incidentStatusOptions" :key="status" :value="status">
                {{ status }}
              </option>
            </select>
          </div>
          <div class="form-field">
            <label>Cost</label>
            <input v-model="incidentForm.cost" type="text" placeholder="e.g., $1,250" />
          </div>
          <div class="form-field">
            <label>Notes</label>
            <input v-model="incidentForm.notes" type="text" placeholder="Summary of incident" />
          </div>
        </div>

        <div class="form-actions">
          <button class="ghost-button" type="button" @click="incidentOpen = false">Cancel</button>
          <button class="primary-button" type="button" @click="saveIncident">
            {{ incidentMode === 'edit' ? 'Save Changes' : 'Save Incident' }}
          </button>
        </div>
      </div>
    </v-dialog>

    <v-dialog v-model="incidentDetailsOpen" max-width="720">
      <div v-if="selectedIncident" class="card-surface details-card">
        <div class="details-header">
          <div>
            <div class="details-title">Incident {{ selectedIncident.id }}</div>
            <div class="details-subtitle text-muted">
              {{ selectedIncident.vehicleId }} - {{ selectedIncident.type }}
            </div>
          </div>
          <button class="icon-button" type="button" @click="incidentDetailsOpen = false">
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>

        <div class="details-grid">
          <div class="details-section">
            <h4>Overview</h4>
            <div class="details-row"><span>Date</span><strong>{{ formatDate(selectedIncident.date) }}</strong></div>
            <div class="details-row"><span>Driver</span><strong>{{ selectedIncident.driver || '—' }}</strong></div>
            <div class="details-row"><span>Status</span><strong>{{ selectedIncident.status }}</strong></div>
            <div class="details-row"><span>Severity</span><strong>{{ selectedIncident.severity }}</strong></div>
          </div>
          <div class="details-section">
            <h4>Claims</h4>
            <div class="details-row"><span>Cost</span><strong>{{ selectedIncident.cost || '—' }}</strong></div>
            <div class="details-row"><span>Notes</span><strong>{{ selectedIncident.notes || '—' }}</strong></div>
          </div>
        </div>
      </div>
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
import { computed, onMounted, ref } from 'vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'
import { statusesApi } from '../../services/tripSetupApi'

const vehicleOptions = [
  { id: 'VH-2048', model: 'Volvo FL 280' },
  { id: 'VH-3054', model: 'Hino 500' },
  { id: 'VH-8120', model: 'Isuzu NPR' },
  { id: 'VH-2661', model: 'Mitsubishi Fuso' }
]

const incidents = ref([
  {
    id: 'INC-1024',
    vehicleId: 'VH-2048',
    driver: 'Sarah Johnson',
    date: '2026-01-24',
    type: 'Minor collision',
    severity: 'Low',
    status: 'Closed',
    cost: '$580',
    notes: 'Rear bumper repair'
  },
  {
    id: 'INC-1091',
    vehicleId: 'VH-3054',
    driver: 'Michael Chen',
    date: '2026-02-18',
    type: 'Windshield crack',
    severity: 'Medium',
    status: 'Open',
    cost: '$1,220',
    notes: 'Awaiting glass replacement'
  }
])

const searchQuery = ref('')
const statusFilter = ref('All')
const statusOptions = ref([])
const severityFilter = ref('All')
const incidentOpen = ref(false)
const incidentMode = ref('add')
const incidentError = ref('')
const incidentForm = ref({})
const incidentDetailsOpen = ref(false)
const selectedIncident = ref(null)
const confirmOpen = ref(false)
const confirmTitle = ref('Are you sure?')
const confirmMessage = ref('')
const confirmButton = ref('Confirm')
const confirmTone = ref('danger')
const pendingAction = ref(() => {})

const incidentHeaders = [
  { title: 'Date', key: 'date', sortable: false },
  { title: 'Vehicle', key: 'vehicle', sortable: false },
  { title: 'Type', key: 'type', sortable: false },
  { title: 'Severity', key: 'severity', sortable: false },
  { title: 'Status', key: 'status', sortable: false },
  { title: 'Cost', key: 'cost', align: 'end', sortable: false },
  { title: 'Actions', key: 'actions', align: 'end', sortable: false }
]

const openCount = computed(() =>
  incidents.value.filter((incident) => incident.status === 'Open').length
)
const closedCount = computed(() =>
  incidents.value.filter((incident) => incident.status === 'Closed').length
)
const highSeverityCount = computed(() =>
  incidents.value.filter((incident) => incident.severity === 'High').length
)

const incidentStatusFilterOptions = computed(() => {
  const values = new Set(statusOptions.value)
  incidents.value.forEach((incident) => {
    if (incident.status) values.add(incident.status)
  })
  return [...values]
})

const incidentStatusOptions = computed(() => {
  const values = new Set(statusOptions.value)
  if (incidentForm.value.status) values.add(incidentForm.value.status)
  return [...values]
})

const filteredIncidents = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()

  return incidents.value.filter((incident) => {
    const matchesSearch = !query || [
      incident.id,
      incident.vehicleId,
      incident.driver,
      incident.type,
      incident.notes
    ].some((value) => String(value || '').toLowerCase().includes(query))

    const matchesStatus = statusFilter.value === 'All' || incident.status === statusFilter.value
    const matchesSeverity = severityFilter.value === 'All' || incident.severity === severityFilter.value

    return matchesSearch && matchesStatus && matchesSeverity
  })
})

const initials = (value) =>
  String(value || '')
    .replace(/[^a-zA-Z0-9]/g, '')
    .slice(-2)
    .toUpperCase()

const severityClass = (severity) => {
  if (severity === 'High') return 'role-admin'
  if (severity === 'Medium') return 'role-mechanic'
  return 'role-driver'
}

const formatDate = (value) =>
  value
    ? new Date(value).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
      })
    : '—'

const buildEmptyIncident = () => ({
  id: '',
  vehicleId: '',
  driver: '',
  date: '',
  type: '',
  severity: 'Low',
  status: statusOptions.value[0] || '',
  cost: '',
  notes: ''
})

const openIncident = () => {
  incidentMode.value = 'add'
  incidentForm.value = buildEmptyIncident()
  incidentError.value = ''
  incidentOpen.value = true
}

const openIncidentEdit = (incident) => {
  incidentMode.value = 'edit'
  incidentForm.value = { ...buildEmptyIncident(), ...incident }
  incidentError.value = ''
  incidentOpen.value = true
}

const openIncidentDetails = (incident) => {
  selectedIncident.value = incident
  incidentDetailsOpen.value = true
}

const saveIncident = () => {
  if (!incidentForm.value.vehicleId || !incidentForm.value.type || !incidentForm.value.date || !incidentForm.value.status) {
    incidentError.value = 'Vehicle, date, type, and status are required.'
    return
  }
  if (incidentMode.value === 'add') {
    const newId = `INC-${Math.floor(1000 + Math.random() * 9000)}`
    incidents.value = [
      {
        ...incidentForm.value,
        id: newId
      },
      ...incidents.value
    ]
  } else {
    incidents.value = incidents.value.map((item) =>
      item.id === incidentForm.value.id ? { ...item, ...incidentForm.value } : item
    )
  }
  incidentOpen.value = false
}

const deleteIncident = (id) => {
  const incident = incidents.value.find((item) => item.id === id)
  if (!incident) return
  openConfirm({
    title: 'Delete Incident?',
    message: `This will permanently remove ${incident.id}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: () => {
      incidents.value = incidents.value.filter((item) => item.id !== id)
    }
  })
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
  pendingAction.value?.()
  confirmOpen.value = false
}

const loadStatusOptions = async () => {
  try {
    statusOptions.value = await statusesApi.options()
  } catch (error) {
    console.error('[incidents] failed to load status options', error)
  }
}

onMounted(loadStatusOptions)
</script>

<style scoped src="../roles/roles_styles/RoleManagementContent.css"></style>
<style scoped src="../roles/roles_styles/RoleTable.css"></style>

<style scoped>
.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.table-card {
  overflow: hidden;
}

.stat-foot,
.vehicle-sub {
  font-size: 12px;
}

.text-warning {
  color: var(--fleet-warning);
}

.text-danger {
  color: var(--fleet-danger);
}

.incident-table :deep(thead th:nth-child(1)),
.incident-table :deep(tbody td:nth-child(1)) {
  width: 150px;
}

.incident-table :deep(thead th:nth-child(2)),
.incident-table :deep(tbody td:nth-child(2)) {
  width: 240px;
}

.incident-table :deep(thead th:nth-child(3)),
.incident-table :deep(tbody td:nth-child(3)) {
  width: 220px;
}

.vehicle-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.vehicle-avatar {
  width: 36px;
  height: 36px;
  border-radius: 50%;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #2563eb, #1d4ed8);
  color: #fff;
  font-size: 12px;
  font-weight: 700;
  flex: 0 0 36px;
}

.ghost-button {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border: 1px solid var(--fleet-border);
  border-radius: 12px;
  padding: 10px 16px;
  background: #fff;
  color: var(--fleet-text);
  font-weight: 600;
  cursor: pointer;
}

.ghost-button:hover {
  background: #f8fafc;
}

.form-card {
  padding: 18px 20px 22px;
  max-height: 80vh;
  overflow-y: auto;
}

.form-header,
.details-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
  padding-bottom: 14px;
  border-bottom: 1px solid var(--fleet-border);
}

.form-title,
.details-title {
  font-weight: 700;
  font-size: 18px;
}

.form-error {
  margin-top: 12px;
  padding: 10px 12px;
  border-radius: 10px;
  background: #fee2e2;
  color: #b91c1c;
  font-size: 13px;
}

.form-grid,
.details-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 12px 16px;
  margin-top: 16px;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 13px;
}

.form-field label {
  color: var(--fleet-muted);
}

.form-field input,
.form-field select {
  border: 1px solid var(--fleet-border);
  border-radius: 10px;
  padding: 9px 12px;
  font-size: 14px;
  background: #fff;
}

.form-field input:focus,
.form-field select:focus {
  outline: 2px solid rgba(37, 99, 235, 0.18);
  border-color: rgba(37, 99, 235, 0.6);
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 18px;
}

.details-card {
  padding: 20px 22px 24px;
}

.details-subtitle {
  font-size: 13px;
  margin-top: 4px;
}

.details-section {
  border: 1px solid var(--fleet-border);
  border-radius: 14px;
  padding: 14px;
  background: #fff;
}

.details-section h4 {
  margin: 0 0 10px;
  font-size: 14px;
}

.details-row {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  font-size: 13px;
  padding: 6px 0;
  border-bottom: 1px dashed #e2e8f0;
}

.details-row:last-child {
  border-bottom: none;
}

@media (max-width: 720px) {
  .toolbar-actions {
    width: 100%;
    flex-direction: column;
    align-items: stretch;
  }
}
</style>
