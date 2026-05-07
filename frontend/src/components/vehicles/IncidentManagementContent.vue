<template>
  <div class="role-page incident-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Incident Management</h1>
        <p class="section-subtitle">Track accident and incident records across the fleet.</p>
      </div>
    </div>

    <PageMessage
      :tone="pageMessage.tone"
      :title="pageMessage.title"
      :message="pageMessage.message"
      @close="clearPageMessage"
    />

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
          <div v-if="showScopeFilter" class="toolbar-filter">
            <v-icon icon="mdi-account-switch-outline" />
            <select v-model="scopeFilter" @change="loadIncidents">
              <option value="mine">My Incidents</option>
              <option value="all">All Incidents</option>
            </select>
          </div>

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
              <option v-for="severity in severityOptions" :key="severity" :value="severity">
                {{ severity }}
              </option>
            </select>
          </div>

          <button v-if="canCreateIncidents" class="primary-button" type="button" @click="openIncident">
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
          :loading="loadingIncidents"
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
              <button v-if="canEditIncidents" class="icon-button tooltip" type="button" @click="openIncidentEdit(item)">
                <v-icon icon="mdi-pencil-outline" size="18" />
                <span class="tooltip-text">Edit incident</span>
              </button>
              <button v-if="canDeleteIncidents" class="icon-button danger tooltip" type="button" @click="deleteIncident(item.id)">
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
            <select v-model="incidentForm.vehicleId" @change="syncSelectedVehicle">
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
            <select v-model="incidentForm.type">
              <option value="" disabled>Select incident type</option>
              <option v-for="type in incidentTypeOptions" :key="type" :value="type">
                {{ type }}
              </option>
            </select>
          </div>
          <div class="form-field">
            <label>Severity</label>
            <select v-model="incidentForm.severity">
              <option v-for="severity in severityOptions" :key="severity" :value="severity">
                {{ severity }}
              </option>
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
import PageMessage from '../common/PageMessage.vue'
import { usePageMessage } from '../../composables/usePageMessage'
import { incidentTypesApi, severitiesApi, statusesApi } from '../../services/tripSetupApi'
import { createIncident, deleteIncident as deleteIncidentRecord, getIncidents, updateIncident } from '../../services/incidentsApi'
import { getVehicles } from '../../services/vehiclesApi'
import { canCreateModule, canDeleteModule, canEditModule, getCurrentUser } from '../../utils/authSession'

const vehicleOptions = ref([])
const incidents = ref([])

const searchQuery = ref('')
const statusFilter = ref('All')
const statusOptions = ref([])
const severityOptions = ref(['Low', 'Medium', 'High'])
const incidentTypeOptions = ref([])
const severityFilter = ref('All')
const loadingIncidents = ref(false)
const incidentOpen = ref(false)
const incidentMode = ref('add')
const incidentError = ref('')
const { pageMessage, clearPageMessage, showPageMessage } = usePageMessage(4000)
const incidentForm = ref({})
const incidentDetailsOpen = ref(false)
const selectedIncident = ref(null)
const confirmOpen = ref(false)
const confirmTitle = ref('Are you sure?')
const confirmMessage = ref('')
const confirmButton = ref('Confirm')
const confirmTone = ref('danger')
const pendingAction = ref(() => {})
const canCreateIncidents = computed(() => canCreateModule('incidents'))
const canEditIncidents = computed(() => canEditModule('incidents'))
const canDeleteIncidents = computed(() => canDeleteModule('incidents'))
const currentUser = computed(() => getCurrentUser())
const currentRole = computed(() => String(currentUser.value?.roleId || currentUser.value?.role || '').toLowerCase())
const showScopeFilter = computed(() => currentRole.value === 'driver')
const scopeFilter = ref(showScopeFilter.value ? 'mine' : 'all')

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
  severity: severityOptions.value[0] || '',
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

const syncSelectedVehicle = () => {
  const selectedVehicle = vehicleOptions.value.find((vehicle) => vehicle.id === incidentForm.value.vehicleId)
  if (!selectedVehicle) return
  incidentForm.value.driver = selectedVehicle.driver || incidentForm.value.driver
}

const saveIncident = async () => {
  if (!incidentForm.value.vehicleId || !incidentForm.value.type || !incidentForm.value.date || !incidentForm.value.status) {
    incidentError.value = 'Vehicle, date, type, and status are required.'
    showPageMessage({
      tone: 'error',
      title: 'Incident was not saved',
      message: incidentError.value
    })
    return
  }
  try {
    incidentError.value = ''
    const isEdit = incidentMode.value === 'edit'
    if (incidentMode.value === 'add') {
      const saved = await createIncident(incidentForm.value)
      incidents.value = [saved, ...incidents.value]
    } else {
      const saved = await updateIncident(incidentForm.value.id, incidentForm.value)
      incidents.value = incidents.value.map((item) => (item.id === saved.id ? saved : item))
      if (selectedIncident.value?.id === saved.id) selectedIncident.value = saved
    }
    incidentOpen.value = false
    showPageMessage({
      tone: 'success',
      title: isEdit ? 'Incident updated' : 'Incident created',
      message: isEdit ? 'Incident record was updated successfully.' : 'Incident record was created successfully.'
    })
  } catch (error) {
    incidentError.value = error.message || 'Unable to save incident.'
    showPageMessage({
      tone: 'error',
      title: 'Incident was not saved',
      message: incidentError.value
    })
  }
}

const deleteIncident = (id) => {
  const incident = incidents.value.find((item) => item.id === id)
  if (!incident) return
  openConfirm({
    title: 'Delete Incident?',
    message: `This will permanently remove ${incident.id}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: async () => {
      try {
        incidentError.value = ''
        await deleteIncidentRecord(id)
        incidents.value = incidents.value.filter((item) => item.id !== id)
        showPageMessage({
          tone: 'success',
          title: 'Incident deleted',
          message: `${incident.id} was deleted successfully.`
        })
      } catch (error) {
        incidentError.value = error.message || 'Unable to delete incident.'
        showPageMessage({
          tone: 'error',
          title: 'Incident was not deleted',
          message: incidentError.value
        })
      }
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

const runConfirm = async () => {
  await pendingAction.value?.()
  confirmOpen.value = false
}

const loadIncidents = async () => {
  loadingIncidents.value = true
  try {
    const result = await getIncidents({ page: 1, pageSize: 500, sortBy: 'date', sortOrder: 'desc', scope: scopeFilter.value })
    incidents.value = result.items || []
  } catch (error) {
    incidentError.value = error.message || 'Unable to load incidents.'
  } finally {
    loadingIncidents.value = false
  }
}

const loadReferenceOptions = async () => {
  try {
    const [statuses, severities, incidentTypes, vehicles] = await Promise.allSettled([
      statusesApi.options(),
      severitiesApi.options(),
      incidentTypesApi.options(),
      getVehicles()
    ])
    if (statuses.status === 'fulfilled') statusOptions.value = statuses.value
    if (severities.status === 'fulfilled' && severities.value.length) severityOptions.value = severities.value
    if (incidentTypes.status === 'fulfilled') incidentTypeOptions.value = incidentTypes.value
    if (vehicles.status === 'fulfilled') vehicleOptions.value = vehicles.value
  } catch (error) {
    console.error('[incidents] failed to load reference options', error)
  }
}

onMounted(async () => {
  await loadReferenceOptions()
  await loadIncidents()
})
</script>

<style scoped src="../roles/roles_styles/RoleManagementContent.css"></style>
<style scoped src="../roles/roles_styles/RoleTable.css"></style>

<style scoped src="./vehicles_styles/IncidentManagementContent.css"></style>
