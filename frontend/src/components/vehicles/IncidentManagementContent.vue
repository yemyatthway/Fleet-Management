<template>
  <div class="incident-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Incident Management</h1>
        <p class="section-subtitle">Track accident & incident records across the fleet</p>
      </div>
      <button class="primary-button" type="button" @click="openIncident">
        <v-icon icon="mdi-alert-circle-outline" size="18" />
        Report Incident
      </button>
    </div>

    <div class="card-surface section-card table-card">
      <div class="section-header">
        <div>
          <div class="section-title">Accident & Incident Records</div>
          <div class="text-muted section-subtitle">Track claims, costs, and follow-ups</div>
        </div>
      </div>
      <div class="table-wrap">
        <table class="table-base">
          <thead>
            <tr>
              <th>Date</th>
              <th>Vehicle</th>
              <th>Type</th>
              <th>Severity</th>
              <th>Status</th>
              <th class="align-right">Cost</th>
              <th class="align-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="incident in pagedIncidents" :key="incident.id">
              <td class="text-muted" data-label="Date">{{ formatDate(incident.date) }}</td>
              <td data-label="Vehicle">
                <strong>{{ incident.vehicleId }}</strong>
                <div class="text-muted vehicle-sub">{{ incident.driver }}</div>
              </td>
              <td data-label="Type">{{ incident.type }}</td>
              <td data-label="Severity">
                <span class="badge" :class="severityClass(incident.severity)">
                  {{ incident.severity }}
                </span>
              </td>
              <td data-label="Status">
                <span class="badge" :class="incident.status === 'Open' ? 'warning' : 'success'">
                  {{ incident.status }}
                </span>
              </td>
              <td class="align-right" data-label="Cost">{{ incident.cost || '—' }}</td>
              <td class="align-right" data-label="Actions">
                <div class="inline-actions">
                  <button class="icon-button tooltip" type="button" @click="openIncidentDetails(incident)">
                    <v-icon icon="mdi-eye-outline" size="18" />
                    <span class="tooltip-text">View details</span>
                  </button>
                  <button class="icon-button tooltip" type="button" @click="openIncidentEdit(incident)">
                    <v-icon icon="mdi-pencil-outline" size="18" />
                    <span class="tooltip-text">Edit incident</span>
                  </button>
                  <button class="icon-button danger tooltip" type="button" @click="deleteIncident(incident.id)">
                    <v-icon icon="mdi-trash-can-outline" size="18" />
                    <span class="tooltip-text">Delete incident</span>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-if="incidentTotalPages > 1" class="table-footer">
        <span class="pager-info text-muted">Page {{ incidentSafePage }} of {{ incidentTotalPages }}</span>
        <div class="pager-actions">
          <button
            class="pager-button"
            type="button"
            :disabled="incidentSafePage === 1"
            @click="incidentPage = incidentSafePage - 1"
          >
            Prev
          </button>
          <button
            class="pager-button"
            type="button"
            :disabled="incidentSafePage === incidentTotalPages"
            @click="incidentPage = incidentSafePage + 1"
          >
            Next
          </button>
        </div>
      </div>
      <div v-if="incidents.length === 0" class="empty-state">
        No incidents recorded yet
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
                {{ vehicle.id }} • {{ vehicle.model }}
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
              <option value="Open">Open</option>
              <option value="Closed">Closed</option>
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
              {{ selectedIncident.vehicleId }} • {{ selectedIncident.type }}
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
import { computed, ref } from 'vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'

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

const incidentPage = ref(1)
const incidentPageSize = 5
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

const incidentTotalPages = computed(() =>
  Math.max(1, Math.ceil(incidents.value.length / incidentPageSize))
)
const incidentSafePage = computed(() => Math.min(incidentPage.value, incidentTotalPages.value))
const pagedIncidents = computed(() => {
  const start = (incidentSafePage.value - 1) * incidentPageSize
  return incidents.value.slice(start, start + incidentPageSize)
})

const severityClass = (severity) => {
  if (severity === 'High') return 'danger'
  if (severity === 'Medium') return 'warning'
  return 'success'
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
  status: 'Open',
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
  if (!incidentForm.value.vehicleId || !incidentForm.value.type || !incidentForm.value.date) {
    incidentError.value = 'Vehicle, date, and type are required.'
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
</script>

<style scoped>
.incident-page {
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

.section-card {
  padding: 18px 20px 22px;
}

.section-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 14px;
  border-bottom: 1px solid var(--fleet-border);
  margin-bottom: 14px;
}

.section-subtitle {
  font-size: 12px;
  margin-top: 4px;
}

.table-wrap {
  overflow-x: auto;
}

.table-card {
  overflow: hidden;
}

.table-footer {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 12px;
  padding: 12px 16px 16px;
  border-top: 1px solid var(--fleet-border);
  flex-wrap: wrap;
}

.pager-actions {
  display: inline-flex;
  gap: 8px;
}

.pager-button {
  border: 1px solid var(--fleet-border);
  background: #fff;
  color: var(--fleet-text);
  font-size: 12px;
  font-weight: 600;
  padding: 6px 12px;
  border-radius: 10px;
  cursor: pointer;
}

.pager-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
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

.badge.danger {
  background: #fee2e2;
  color: #b91c1c;
}

.align-right {
  text-align: right;
}

.vehicle-sub {
  font-size: 12px;
  margin-top: 2px;
}

.inline-actions {
  display: inline-flex;
  gap: 8px;
}

.empty-state {
  text-align: center;
  padding: 32px;
  color: var(--fleet-muted);
}

.details-card {
  padding: 20px 22px 24px;
}

.details-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--fleet-border);
}

.details-title {
  font-size: 18px;
  font-weight: 700;
}

.details-subtitle {
  font-size: 13px;
  margin-top: 4px;
}

.details-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
  padding-top: 18px;
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

.form-card {
  padding: 18px 20px 22px;
  max-height: 80vh;
  overflow-y: auto;
  overflow-x: hidden;
}

.form-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--fleet-border);
}

.form-title {
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

.form-grid {
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

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 18px;
}

.tooltip {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: help;
}

.tooltip:focus-visible {
  outline: 2px solid rgba(37, 99, 235, 0.35);
  outline-offset: 2px;
}

.tooltip-text {
  position: absolute;
  bottom: calc(100% + 8px);
  left: 0;
  transform: translate(0, 6px);
  background: #0f172a;
  color: #fff;
  padding: 6px 8px;
  border-radius: 8px;
  font-size: 12px;
  white-space: nowrap;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.15s ease, transform 0.15s ease;
  box-shadow: 0 8px 16px rgba(15, 23, 42, 0.2);
  z-index: 2;
}

.tooltip:hover .tooltip-text,
.tooltip:focus-visible .tooltip-text {
  opacity: 1;
  transform: translate(0, 0);
}

@media (max-width: 980px) {
  .page-header {
    flex-direction: column;
    align-items: flex-start;
  }

  .primary-button {
    width: 100%;
    justify-content: center;
  }
}
</style>
