<template>
  <div class="vehicle-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Vehicle Management</h1>
        <p class="section-subtitle">Track, assign, and maintain your fleet in one place</p>
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
        <p>Total Vehicles</p>
        <h3>{{ vehicles.length }}</h3>
        <span class="stat-foot text-muted">Fleet size</span>
      </div>
      <div class="stat-card">
        <p>Active</p>
        <h3 class="text-success">{{ activeCount }}</h3>
        <span class="stat-foot text-muted">On the road</span>
      </div>
      <div class="stat-card">
        <p>In Maintenance</p>
        <h3 class="text-warning">{{ maintenanceCount }}</h3>
        <span class="stat-foot text-muted">Scheduled service</span>
      </div>
      <div class="stat-card">
        <p>Inactive</p>
        <h3 class="text-danger">{{ inactiveCount }}</h3>
        <span class="stat-foot text-muted">Unavailable</span>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search by vehicle ID, plate, or driver..."
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
            <select v-model="scopeFilter" @change="loadVehicles">
              <option value="mine">My Vehicles</option>
              <option value="all">All Vehicles</option>
            </select>
          </div>

          <div class="toolbar-filter">
            <v-icon icon="mdi-filter-variant" />
            <select v-model="statusFilter">
              <option value="All">All Status</option>
              <option v-for="status in statusOptions" :key="status" :value="status">
                {{ status }}
              </option>
            </select>
          </div>

          <button v-if="canCreateVehicles" class="primary-button" type="button" @click="openAdd">
            <v-icon icon="mdi-truck-plus" size="18" />
            Add Vehicle
          </button>
        </div>
      </div>

      <div class="toolbar-count text-muted">
        Showing {{ filteredVehicles.length }} of {{ vehicles.length }} vehicles
      </div>
    </div>

    <div v-if="formError && !formOpen" class="form-error page-error">{{ formError }}</div>

    <div class="card-surface table-card">
      <div class="table-wrap">
        <v-data-table
          class="table-base vehicle-table"
          :headers="vehicleHeaders"
          :items="filteredVehicles"
          :items-per-page="10"
          :items-per-page-options="[10, 20, 30]"
          :loading="loading"
          :mobile-breakpoint="0"
          :mobile="false"
          fixed-header
          height="520"
          density="comfortable"
        >
          <template #item.id="{ item }">
            <div class="vehicle-cell">
              <button
                class="thumb-button tooltip"
                :disabled="!item.image"
                type="button"
                @click="openImage(item.image, item.type)"
              >
                <img v-if="item.image" :src="item.image" :alt="item.type" class="vehicle-image" />
                <span v-else class="vehicle-image image-placeholder"><v-icon icon="mdi-truck-outline" size="20" /></span>
                <span v-if="item.image" class="tooltip-text">View vehicle image</span>
              </button>
              <div>
                <strong>{{ item.id }}</strong>
                <div class="text-muted vehicle-sub">{{ item.model }}</div>
              </div>
            </div>
          </template>

          <template #item.plate="{ item }">
            <span class="text-muted">{{ item.plate }}</span>
          </template>

          <template #item.type="{ item }">
            <span>{{ item.type }}</span>
          </template>

          <template #item.status="{ item }">
            <span class="badge" :class="statusClass(item.status)">
              {{ item.status }}
            </span>
          </template>

          <template #item.driver="{ item }">
            <div class="driver-cell">
              <button
                class="thumb-button tooltip"
                :disabled="!item.driverImage"
                type="button"
                @click="openImage(item.driverImage, item.driver)"
              >
                <img v-if="item.driverImage" :src="item.driverImage" :alt="item.driver" class="driver-photo" />
                <span v-else class="driver-photo image-placeholder"><v-icon icon="mdi-account-outline" size="18" /></span>
                <span v-if="item.driverImage" class="tooltip-text">View driver image</span>
              </button>
              <span>{{ item.driver }}</span>
            </div>
          </template>

          <template #item.acquiredDate="{ item }">
            <span class="text-muted">{{ formatDate(item.acquiredDate) }}</span>
          </template>

          <template #item.actions="{ item }">
            <div class="inline-actions">
              <button v-if="canEditVehicles" class="icon-button tooltip" type="button" @click="openEdit(item)">
                <v-icon icon="mdi-pencil-outline" size="18" />
                <span class="tooltip-text">Edit vehicle</span>
              </button>
              <button class="icon-button tooltip" type="button" @click="openDetails(item)">
                <v-icon icon="mdi-eye-outline" size="18" />
                <span class="tooltip-text">View details</span>
              </button>
              <button
                v-if="canEditVehicles"
                class="icon-button tooltip"
                :class="item.status === 'Active' ? 'warn' : 'good'"
                type="button"
                @click="toggleStatus(item.id)"
              >
                <v-icon icon="mdi-power" size="18" />
                <span class="tooltip-text">
                  {{ item.status === 'Active' ? 'Set inactive' : 'Set active' }}
                </span>
              </button>
              <button v-if="canDeleteVehicles" class="icon-button danger tooltip" type="button" @click="deleteVehicle(item.id)">
                <v-icon icon="mdi-trash-can-outline" size="18" />
                <span class="tooltip-text">Delete vehicle</span>
              </button>
            </div>
          </template>

          <template #no-data>
            <div class="empty-state">No vehicles found matching your criteria</div>
          </template>
        </v-data-table>
      </div>
    </div>

    <v-dialog v-model="detailsOpen" max-width="960">
      <div v-if="selectedVehicle" class="card-surface details-card">
        <div class="details-header">
          <div>
            <div class="details-title">
              {{ selectedVehicle.id }} • {{ selectedVehicle.plate }}
            </div>
            <div class="details-subtitle text-muted">
              {{ selectedVehicle.model }} • {{ selectedVehicle.type }}
            </div>
          </div>
          <button class="icon-button" type="button" @click="detailsOpen = false">
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>

        <div class="details-grid">
          <div class="details-section">
            <h4>Overview</h4>
            <div class="details-row"><span>Region</span><strong>{{ selectedVehicle.region }}</strong></div>
            <div class="details-row"><span>Driver</span><strong>{{ selectedVehicle.driver }}</strong></div>
            <div class="details-row"><span>Depot</span><strong>{{ selectedVehicle.depot }}</strong></div>
            <div class="details-row"><span>Status</span><strong>{{ selectedVehicle.status }}</strong></div>
            <div class="details-row"><span>Capacity</span><strong>{{ selectedVehicle.capacity }}</strong></div>
            <div class="details-row"><span>Fuel Type</span><strong>{{ selectedVehicle.fuelType }}</strong></div>
          </div>

          <div class="details-section">
            <h4>Identity</h4>
            <div class="details-row"><span>VIN / Chassis</span><strong>{{ selectedVehicle.vin }}</strong></div>
            <div class="details-row"><span>Engine No.</span><strong>{{ selectedVehicle.engineNo }}</strong></div>
            <div class="details-row"><span>Odometer</span><strong>{{ selectedVehicle.odometer }}</strong></div>
            <div class="details-row"><span>Acquired</span><strong>{{ formatDate(selectedVehicle.acquiredDate) }}</strong></div>
          </div>

          <div class="details-section">
            <h4>Specs & Ownership</h4>
            <div class="details-row"><span>Make</span><strong>{{ selectedVehicle.make || '—' }}</strong></div>
            <div class="details-row"><span>Year</span><strong>{{ selectedVehicle.year || '—' }}</strong></div>
            <div class="details-row"><span>Color</span><strong>{{ selectedVehicle.color || '—' }}</strong></div>
            <div class="details-row"><span>Ownership</span><strong>{{ selectedVehicle.ownership || '—' }}</strong></div>
            <div class="details-row"><span>Purchase Cost</span><strong>{{ selectedVehicle.purchaseCost || '—' }}</strong></div>
            <div class="details-row"><span>Fuel Capacity</span><strong>{{ selectedVehicle.fuelCapacity || '—' }}</strong></div>
          </div>

          <div class="details-section">
            <h4>Compliance</h4>
            <div class="details-row"><span>Registration No.</span><strong>{{ selectedVehicle.registrationNo || '—' }}</strong></div>
            <div class="details-row"><span>Registration Expiry</span><strong>{{ formatDate(selectedVehicle.registrationExpiry) }}</strong></div>
            <div class="details-row"><span>Road Tax Expiry</span><strong>{{ formatDate(selectedVehicle.roadTaxExpiry) }}</strong></div>
            <div class="details-row"><span>Insurance Expiry</span><strong>{{ formatDate(selectedVehicle.insuranceExpiry) }}</strong></div>
            <div class="details-row"><span>Insurance Provider</span><strong>{{ selectedVehicle.insuranceProvider || '—' }}</strong></div>
            <div class="details-row"><span>Policy No.</span><strong>{{ selectedVehicle.insurancePolicy || '—' }}</strong></div>
            <div class="details-row"><span>Inspection Due</span><strong>{{ formatDate(selectedVehicle.inspectionDue) }}</strong></div>
          </div>

          <div class="details-section">
            <h4>Maintenance</h4>
            <div class="details-row"><span>Last Service</span><strong>{{ formatDate(selectedVehicle.lastService) }}</strong></div>
            <div class="details-row"><span>Next Service</span><strong>{{ formatDate(selectedVehicle.nextService) }}</strong></div>
            <div class="details-row"><span>Service Note</span><strong>{{ selectedVehicle.serviceNote }}</strong></div>
          </div>

        </div>
      </div>
    </v-dialog>

    <v-dialog v-model="formOpen" max-width="960">
      <div class="card-surface form-card">
        <div class="form-header">
          <div class="form-title">{{ formMode === 'edit' ? 'Edit Vehicle' : 'Add Vehicle' }}</div>
          <button class="icon-button" type="button" @click="closeForm">
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>

        <div v-if="formError" class="form-error">{{ formError }}</div>

        <div class="form-steps">
          <div
            v-for="step in formSteps"
            :key="step.id"
            class="form-step"
            :class="{ active: formStep === step.id, done: formStep > step.id }"
          >
            <div class="step-index">{{ step.id }}</div>
            <div>
              <div class="step-title">{{ step.title }}</div>
              <div class="text-muted step-subtitle">{{ step.subtitle }}</div>
            </div>
          </div>
        </div>

        <div v-if="formStep === 1" class="form-grid">
          <div class="form-field">
            <label>Plate Number <span class="required-mark">*</span></label>
            <input v-model="formData.plate" type="text" placeholder="e.g., YGN-7742" />
          </div>
          <div class="form-field">
            <label>Region <span class="required-mark">*</span></label>
            <input v-model="formData.region" type="text" placeholder="e.g., Yangon" />
          </div>
          <div class="form-field">
            <label>Vehicle Type <span class="required-mark">*</span></label>
            <select v-model="formData.type">
              <option value="" disabled>Select vehicle type</option>
              <option v-for="type in availableVehicleTypeOptions" :key="type" :value="type">
                {{ type }}
              </option>
            </select>
          </div>
          <div class="form-field">
            <label>Model <span class="required-mark">*</span></label>
            <input v-model="formData.model" type="text" placeholder="e.g., Isuzu FVR" />
          </div>
          <div class="form-field">
            <label>Make</label>
            <input v-model="formData.make" type="text" placeholder="e.g., Isuzu" />
          </div>
          <div class="form-field">
            <label>Year</label>
            <input v-model="formData.year" type="number" min="1980" max="2100" placeholder="e.g., 2022" />
          </div>
          <div class="form-field">
            <label>Color</label>
            <input v-model="formData.color" type="text" placeholder="e.g., White" />
          </div>
          <div class="form-field">
            <label>Status <span class="required-mark">*</span></label>
            <select v-model="formData.status">
              <option value="" disabled>Select status</option>
              <option v-for="status in availableStatusOptions" :key="status" :value="status">
                {{ status }}
              </option>
            </select>
          </div>
          <div class="form-field">
            <label>Ownership</label>
            <select v-model="formData.ownership">
              <option value="Owned">Owned</option>
              <option value="Leased">Leased</option>
            </select>
          </div>
          <div class="form-field">
            <label>Driver <span class="required-mark">*</span></label>
            <select v-model="formData.driver" @change="syncSelectedDriverImage">
              <option value="" disabled>Select driver</option>
              <option v-for="driver in availableDriverOptions" :key="driver.name" :value="driver.name">
                {{ driver.name }}
              </option>
            </select>
          </div>
          <div class="form-field">
            <label>
              Depot
              <span class="hint tooltip icon-tooltip" tabindex="0" aria-label="Home base or yard">
                <v-icon icon="mdi-information-outline" size="14" />
                <span class="tooltip-text">Home base / yard</span>
              </span>
            </label>
            <select v-model="formData.depot">
              <option value="">Select depot</option>
              <option v-for="depot in availableDepotOptions" :key="depot" :value="depot">
                {{ depot }}
              </option>
            </select>
          </div>
          <div class="form-field">
            <label>Capacity</label>
            <input v-model="formData.capacity" type="text" placeholder="e.g., 6 tons" />
          </div>
          <div class="form-field">
            <label>Fuel Capacity</label>
            <input v-model="formData.fuelCapacity" type="text" placeholder="e.g., 120 L" />
          </div>
          <div class="form-field">
            <label>Fuel Type <span class="required-mark">*</span></label>
            <select v-model="formData.fuelType">
              <option value="" disabled>Select fuel type</option>
              <option v-for="type in availableFuelTypeOptions" :key="type" :value="type">
                {{ type }}
              </option>
            </select>
          </div>
          <div class="form-field">
            <label>VIN / Chassis</label>
            <input v-model="formData.vin" type="text" placeholder="VIN / chassis" />
          </div>
          <div class="form-field">
            <label>Engine No.</label>
            <input v-model="formData.engineNo" type="text" placeholder="Engine number" />
          </div>
          <div class="form-field">
            <label>
              Odometer
              <span
                class="hint tooltip icon-tooltip"
                tabindex="0"
                aria-label="Total distance traveled in kilometers"
              >
                <v-icon icon="mdi-information-outline" size="14" />
                <span class="tooltip-text">Total distance (km)</span>
              </span>
            </label>
            <input v-model="formData.odometer" type="text" placeholder="e.g., 120,000 km" />
          </div>
          <div class="form-field">
            <label>Acquired Date</label>
            <input v-model="formData.acquiredDate" type="date" />
          </div>
        </div>

        <div v-if="formStep === 2" class="form-grid">
          <div class="form-field">
            <label>Purchase Cost</label>
            <input v-model="formData.purchaseCost" type="text" placeholder="e.g., $48,000" />
          </div>
          <div class="form-field">
            <label>Registration Number</label>
            <input v-model="formData.registrationNo" type="text" placeholder="Registration number" />
          </div>
          <div class="form-field">
            <label>
              Registration Expiry
              <span
                class="hint tooltip icon-tooltip"
                tabindex="0"
                aria-label="Date the registration must be renewed"
              >
                <v-icon icon="mdi-information-outline" size="14" />
                <span class="tooltip-text">Renewal date</span>
              </span>
            </label>
            <input v-model="formData.registrationExpiry" type="date" />
          </div>
          <div class="form-field">
            <label>Road Tax Expiry</label>
            <input v-model="formData.roadTaxExpiry" type="date" />
          </div>
          <div class="form-field">
            <label>Insurance Expiry</label>
            <input v-model="formData.insuranceExpiry" type="date" />
          </div>
          <div class="form-field">
            <label>Insurance Provider</label>
            <input v-model="formData.insuranceProvider" type="text" placeholder="Provider name" />
          </div>
          <div class="form-field">
            <label>Policy Number</label>
            <input v-model="formData.insurancePolicy" type="text" placeholder="Policy / certificate no." />
          </div>
          <div class="form-field">
            <label>
              Inspection Due
              <span
                class="hint tooltip icon-tooltip"
                tabindex="0"
                aria-label="Next required safety inspection"
              >
                <v-icon icon="mdi-information-outline" size="14" />
                <span class="tooltip-text">Next safety check</span>
              </span>
            </label>
            <input v-model="formData.inspectionDue" type="date" />
          </div>
          <div class="form-field">
            <label>Last Service</label>
            <input v-model="formData.lastService" type="date" />
          </div>
          <div class="form-field">
            <label>Next Service</label>
            <input v-model="formData.nextService" type="date" />
          </div>
          <div class="form-field">
            <label>Service Note</label>
            <input v-model="formData.serviceNote" type="text" placeholder="Service note" />
          </div>
        </div>

        <div v-if="formStep === 3" class="form-grid">
          <div class="form-field">
            <label>Vehicle Image</label>
            <div class="file-row">
              <input ref="vehicleImageInput" type="file" accept="image/*" @change="handleVehicleImageUpload" />
              <button
                v-if="formData.image"
                class="icon-button"
                type="button"
                @click="handleVehicleImageRemove"
              >
                <v-icon icon="mdi-close" size="16" />
              </button>
            </div>
            <img v-if="formData.image" class="image-preview" :src="formData.image" alt="Vehicle preview" />
          </div>
          <div class="form-field">
            <label>Driver Image</label>
            <div class="file-row">
              <input ref="driverImageInput" type="file" accept="image/*" @change="handleDriverImageUpload" />
              <button
                v-if="formData.driverImage"
                class="icon-button"
                type="button"
                @click="handleDriverImageRemove"
              >
                <v-icon icon="mdi-close" size="16" />
              </button>
            </div>
            <img v-if="formData.driverImage" class="image-preview" :src="formData.driverImage" alt="Driver preview" />
          </div>
        </div>

        <div class="form-actions">
          <button class="ghost-button" type="button" @click="closeForm">Cancel</button>
          <button class="ghost-button" type="button" @click="prevFormStep" :disabled="formStep === 1">
            Back
          </button>
          <button
            v-if="formStep < formSteps.length"
            class="primary-button"
            type="button"
            @click="nextFormStep"
            :disabled="!canGoNext"
          >
            Next
          </button>
          <button
            v-else
            class="primary-button"
            type="button"
            @click="saveForm"
            :disabled="!canSubmit"
          >
            {{ formMode === 'edit' ? 'Save Changes' : 'Add Vehicle' }}
          </button>
        </div>
      </div>
    </v-dialog>

    <v-dialog v-model="imageOpen" max-width="720">
      <div class="card-surface image-modal">
        <div class="image-header">
          <div class="image-title">{{ imageTitle }}</div>
          <button class="icon-button" type="button" @click="imageOpen = false">
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>
        <img v-if="imageSrc" :src="imageSrc" :alt="imageTitle" class="full-image" />
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
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'
import PageMessage from '../common/PageMessage.vue'
import { usePageMessage } from '../../composables/usePageMessage'
import { getFuelTypeOptions } from '../../services/fuelTypesApi'
import { getLocationOptions } from '../../services/locationsApi'
import { statusesApi } from '../../services/tripSetupApi'
import { getUsers } from '../../services/usersApi'
import { getVehicleTypeOptions } from '../../services/vehicleTypesApi'
import { canCreateModule, canDeleteModule, canEditModule, getCurrentUser } from '../../utils/authSession'
import {
  createVehicle,
  deleteVehicle as deleteVehicleRecord,
  getVehicles,
  updateVehicle,
  updateVehicleStatus
} from '../../services/vehiclesApi'

const vehicles = ref([])
const loading = ref(false)
const canCreateVehicles = computed(() => canCreateModule('vehicles'))
const canEditVehicles = computed(() => canEditModule('vehicles'))
const canDeleteVehicles = computed(() => canDeleteModule('vehicles'))
const currentUser = computed(() => getCurrentUser())
const currentRole = computed(() => String(currentUser.value?.roleId || currentUser.value?.role || '').toLowerCase())
const showScopeFilter = computed(() => currentRole.value === 'driver')

const searchQuery = ref('')
const debouncedVehicleQuery = ref('')
const scopeFilter = ref(showScopeFilter.value ? 'mine' : 'all')
const statusFilter = ref('All')
const confirmOpen = ref(false)
const confirmTitle = ref('Are you sure?')
const confirmMessage = ref('')
const confirmButton = ref('Confirm')
const confirmTone = ref('danger')
const pendingAction = ref(() => {})
const detailsOpen = ref(false)
const selectedVehicle = ref(null)
const imageOpen = ref(false)
const imageSrc = ref('')
const imageTitle = ref('')
const formOpen = ref(false)
const formMode = ref('add')
const formError = ref('')
const { pageMessage, clearPageMessage, showPageMessage } = usePageMessage(4000)
const formData = ref({})
const formStep = ref(1)
const vehicleImageInput = ref(null)
const driverImageInput = ref(null)
const fuelTypeOptions = ref(['Diesel', 'Gasoline', 'Electric', 'Hybrid'])
const vehicleTypeOptions = ref([])
const statusOptions = ref([])
const driverOptions = ref([])
const depotOptions = ref([])
const formSteps = [
  { id: 1, title: 'Core Info', subtitle: 'Identity, ownership, assignment' },
  { id: 2, title: 'Compliance', subtitle: 'Registration, insurance, service' },
  { id: 3, title: 'Images', subtitle: 'Vehicle and driver photos' }
]
const filteredVehicles = computed(() => {
  const query = debouncedVehicleQuery.value.toLowerCase()
  return vehicles.value.filter((vehicle) => {
    const matchesSearch =
      vehicle.id.toLowerCase().includes(query) ||
      vehicle.plate.toLowerCase().includes(query) ||
      vehicle.driver.toLowerCase().includes(query) ||
      vehicle.type.toLowerCase().includes(query) ||
      vehicle.model.toLowerCase().includes(query)
    const matchesStatus = statusFilter.value === 'All' || vehicle.status === statusFilter.value
    return matchesSearch && matchesStatus
  })
})

let vehicleSearchTimer = null
watch(
  () => searchQuery.value,
  (value) => {
    if (vehicleSearchTimer) clearTimeout(vehicleSearchTimer)
    vehicleSearchTimer = setTimeout(() => {
      debouncedVehicleQuery.value = value
    }, 350)
  },
  { immediate: true }
)

onBeforeUnmount(() => {
  if (vehicleSearchTimer) clearTimeout(vehicleSearchTimer)
})

const vehicleHeaders = [
  { title: 'Vehicle / ID', key: 'id', sortable: true },
  { title: 'Plate Number', key: 'plate', sortable: false },
  { title: 'Type', key: 'type', sortable: true },
  { title: 'Status', key: 'status', sortable: false },
  { title: 'Driver Assigned', key: 'driver', sortable: false },
  { title: 'Acquired Date', key: 'acquiredDate', sortable: false },
  { title: 'Actions', key: 'actions', align: 'end', sortable: false }
]

const activeCount = computed(() => vehicles.value.filter((v) => v.status === 'Active').length)
const maintenanceCount = computed(() => vehicles.value.filter((v) => v.status === 'Maintenance').length)
const inactiveCount = computed(() => vehicles.value.filter((v) => v.status === 'Inactive').length)

const stepOneValid = computed(
  () =>
    !!formData.value.plate &&
    !!formData.value.region &&
    !!formData.value.type &&
    !!formData.value.model &&
    !!formData.value.status &&
    !!formData.value.driver &&
    !!formData.value.fuelType
)

const canGoNext = computed(() => (formStep.value === 1 ? stepOneValid.value : true))

const canSubmit = computed(() => stepOneValid.value)

const optionsWithCurrentValue = (options, currentValue) => {
  if (!currentValue || options.includes(currentValue)) return options
  return [currentValue, ...options]
}

const availableVehicleTypeOptions = computed(() =>
  optionsWithCurrentValue(vehicleTypeOptions.value, formData.value.type)
)
const availableDriverOptions = computed(() =>
  driverOptions.value.some((driver) => driver.name === formData.value.driver) || !formData.value.driver
    ? driverOptions.value
    : [{ name: formData.value.driver, avatar: formData.value.driverImage }, ...driverOptions.value]
)
const availableDepotOptions = computed(() =>
  optionsWithCurrentValue(depotOptions.value, formData.value.depot)
)
const availableFuelTypeOptions = computed(() =>
  optionsWithCurrentValue(fuelTypeOptions.value, formData.value.fuelType)
)
const availableStatusOptions = computed(() =>
  optionsWithCurrentValue(statusOptions.value, formData.value.status)
)

const statusClass = (status) => {
  if (status === 'Active') return 'success'
  if (status === 'Maintenance') return 'warning'
  return 'neutral'
}

const formatDate = (value) =>
  value
    ? new Date(value).toLocaleDateString('en-US', {
      month: 'short',
      day: 'numeric',
      year: 'numeric'
    })
    : '—'

const openDetails = (vehicle) => {
  selectedVehicle.value = vehicle
  detailsOpen.value = true
}

const openImage = (src, title) => {
  if (!src) return
  imageSrc.value = src
  imageTitle.value = title
  imageOpen.value = true
}

const buildEmptyForm = () => ({
  id: '',
  plate: '',
  region: '',
  type: '',
  model: '',
  make: '',
  year: '',
  color: '',
  status: statusOptions.value[0] || '',
  ownership: 'Owned',
  driver: '',
  driverImage: '',
  depot: '',
  capacity: '',
  fuelCapacity: '',
  fuelType: '',
  vin: '',
  engineNo: '',
  odometer: '',
  lastService: '',
  nextService: '',
  serviceNote: '',
  purchaseCost: '',
  registrationNo: '',
  registrationExpiry: '',
  roadTaxExpiry: '',
  insuranceExpiry: '',
  insuranceProvider: '',
  insurancePolicy: '',
  inspectionDue: '',
  acquiredDate: '',
  image: '',
  vehicleImageFile: null,
  driverImageFile: null,
  removeVehicleImage: false,
  removeDriverImage: false
})

const loadVehicles = async () => {
  loading.value = true
  try {
    vehicles.value = await getVehicles({ scope: scopeFilter.value })
    formError.value = ''
  } catch (error) {
    console.error('[vehicles] failed to load vehicles', error)
    formError.value = error.message || 'Unable to load vehicles.'
  } finally {
    loading.value = false
  }
}

const loadReferenceOptions = async () => {
  const [fuelTypes, vehicleTypes, statuses, drivers, depots] = await Promise.allSettled([
    getFuelTypeOptions(),
    getVehicleTypeOptions(),
    statusesApi.options(),
    getUsers({ role: 'Driver', status: 'Active', pageSize: 500, sortBy: 'name' }),
    getLocationOptions()
  ])

  if (fuelTypes.status === 'fulfilled' && fuelTypes.value.length) fuelTypeOptions.value = fuelTypes.value
  if (vehicleTypes.status === 'fulfilled') vehicleTypeOptions.value = vehicleTypes.value
  if (statuses.status === 'fulfilled') statusOptions.value = statuses.value
  if (drivers.status === 'fulfilled') driverOptions.value = drivers.value.items || []
  if (depots.status === 'fulfilled') depotOptions.value = depots.value

  const rejected = [fuelTypes, vehicleTypes, statuses, drivers, depots].find((result) => result.status === 'rejected')
  if (rejected) {
    console.error('[vehicles] failed to load reference options', rejected.reason)
  }
}

onMounted(() => {
  loadVehicles()
  loadReferenceOptions()
})

const openAdd = () => {
  formMode.value = 'add'
  formData.value = buildEmptyForm()
  loadReferenceOptions()
  formError.value = ''
  formStep.value = 1
  formOpen.value = true
}

const openEdit = (vehicle) => {
  formMode.value = 'edit'
  formData.value = { ...buildEmptyForm(), ...vehicle }
  loadReferenceOptions()
  formError.value = ''
  formStep.value = 1
  formOpen.value = true
}

const closeForm = () => {
  formOpen.value = false
}

const nextFormStep = () => {
  if (formStep.value < formSteps.length && canGoNext.value) {
    formStep.value += 1
  } else if (!canGoNext.value) {
    formError.value = 'Plate, region, type, model, status, driver, and fuel type are required to continue.'
  }
}

const prevFormStep = () => {
  if (formStep.value > 1) {
    formStep.value -= 1
  }
}

const toVehiclePayload = (vehicle) => ({
  plate: vehicle.plate,
  region: vehicle.region,
  type: vehicle.type,
  model: vehicle.model,
  make: vehicle.make,
  year: vehicle.year ? String(vehicle.year) : '',
  color: vehicle.color,
  status: vehicle.status,
  ownership: vehicle.ownership,
  driver: vehicle.driver,
  driverImage: vehicle.driverImage,
  depot: vehicle.depot,
  capacity: vehicle.capacity,
  fuelCapacity: vehicle.fuelCapacity,
  fuelType: vehicle.fuelType,
  vin: vehicle.vin,
  engineNo: vehicle.engineNo,
  odometer: vehicle.odometer,
  lastService: vehicle.lastService,
  nextService: vehicle.nextService,
  serviceNote: vehicle.serviceNote,
  purchaseCost: vehicle.purchaseCost,
  registrationNo: vehicle.registrationNo,
  registrationExpiry: vehicle.registrationExpiry,
  roadTaxExpiry: vehicle.roadTaxExpiry,
  insuranceExpiry: vehicle.insuranceExpiry,
  insuranceProvider: vehicle.insuranceProvider,
  insurancePolicy: vehicle.insurancePolicy,
  inspectionDue: vehicle.inspectionDue,
  acquiredDate: vehicle.acquiredDate,
  vehicleImageFile: vehicle.vehicleImageFile,
  driverImageFile: vehicle.driverImageFile,
  removeVehicleImage: vehicle.removeVehicleImage,
  removeDriverImage: vehicle.removeDriverImage
})

const readImageFile = (file, callback) => {
  const reader = new FileReader()
  reader.onload = (event) => {
    callback(event.target?.result || '')
  }
  reader.readAsDataURL(file)
}

const handleVehicleImageUpload = (event) => {
  const file = event.target.files?.[0]
  if (!file) return
  formData.value.vehicleImageFile = file
  formData.value.removeVehicleImage = false
  readImageFile(file, (value) => {
    formData.value.image = value
  })
}

const handleDriverImageUpload = (event) => {
  const file = event.target.files?.[0]
  if (!file) return
  formData.value.driverImageFile = file
  formData.value.removeDriverImage = false
  readImageFile(file, (value) => {
    formData.value.driverImage = value
  })
}

const syncSelectedDriverImage = () => {
  const selectedDriver = driverOptions.value.find((driver) => driver.name === formData.value.driver)
  if (!selectedDriver?.avatar) return
  formData.value.driverImage = selectedDriver.avatar
  formData.value.driverImageFile = null
  formData.value.removeDriverImage = false
  if (driverImageInput.value) driverImageInput.value.value = ''
}

const handleVehicleImageRemove = () => {
  formData.value.image = ''
  formData.value.vehicleImageFile = null
  formData.value.removeVehicleImage = true
  if (vehicleImageInput.value) vehicleImageInput.value.value = ''
}

const handleDriverImageRemove = () => {
  formData.value.driverImage = ''
  formData.value.driverImageFile = null
  formData.value.removeDriverImage = true
  if (driverImageInput.value) driverImageInput.value.value = ''
}

const saveForm = async () => {
  if (
    !formData.value.plate ||
    !formData.value.region ||
    !formData.value.type ||
    !formData.value.model ||
    !formData.value.status ||
    !formData.value.driver ||
    !formData.value.fuelType
  ) {
    formError.value = 'Plate, region, type, model, status, driver, and fuel type are required.'
    formStep.value = 1
    showPageMessage({
      tone: 'error',
      title: 'Vehicle was not saved',
      message: formError.value
    })
    return
  }

  loading.value = true
  formError.value = ''
  try {
    const payload = toVehiclePayload(formData.value)
    const isEdit = formMode.value === 'edit'

    const savedVehicle = formMode.value === 'add'
      ? await createVehicle(payload)
      : await updateVehicle(formData.value.id, payload)

    if (formMode.value === 'add') {
      vehicles.value = [savedVehicle, ...vehicles.value]
    } else {
      vehicles.value = vehicles.value.map((item) =>
        item.id === savedVehicle.id ? savedVehicle : item
      )
      if (selectedVehicle.value?.id === savedVehicle.id) selectedVehicle.value = savedVehicle
    }

    formOpen.value = false
    showPageMessage({
      tone: 'success',
      title: isEdit ? 'Vehicle updated' : 'Vehicle created',
      message: `${savedVehicle.id} was ${isEdit ? 'updated' : 'created'} successfully.`
    })
  } catch (error) {
    formError.value = error.message || 'Unable to save vehicle.'
    showPageMessage({
      tone: 'error',
      title: 'Vehicle was not saved',
      message: formError.value
    })
  } finally {
    loading.value = false
  }
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
  await pendingAction.value()
  confirmOpen.value = false
}

const toggleStatus = (id) => {
  const vehicle = vehicles.value.find((item) => item.id === id)
  if (!vehicle) return
  const nextStatus = vehicle.status === 'Active' ? 'Inactive' : 'Active'
  openConfirm({
    title: `${nextStatus} Vehicle?`,
    message: `This will mark ${vehicle.id} as ${nextStatus.toLowerCase()}.`,
    confirmText: nextStatus,
    tone: 'warning',
    action: async () => {
      loading.value = true
      try {
        const savedVehicle = await updateVehicleStatus(id, nextStatus)
        formError.value = ''
        vehicles.value = vehicles.value.map((item) =>
          item.id === id ? savedVehicle : item
        )
        if (selectedVehicle.value?.id === id) selectedVehicle.value = savedVehicle
        showPageMessage({
          tone: 'success',
          title: 'Vehicle status updated',
          message: `${savedVehicle.id} was marked ${nextStatus.toLowerCase()}.`
        })
      } catch (error) {
        formError.value = error.message || 'Unable to update vehicle status.'
        showPageMessage({
          tone: 'error',
          title: 'Vehicle status was not updated',
          message: formError.value
        })
      } finally {
        loading.value = false
      }
    }
  })
}

const deleteVehicle = (id) => {
  const vehicle = vehicles.value.find((item) => item.id === id)
  if (!vehicle) return
  openConfirm({
    title: 'Delete Vehicle?',
    message: `This will permanently remove ${vehicle.id}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: async () => {
      loading.value = true
      try {
        await deleteVehicleRecord(id)
        formError.value = ''
        vehicles.value = vehicles.value.filter((item) => item.id !== id)
        if (selectedVehicle.value?.id === id) {
          selectedVehicle.value = null
          detailsOpen.value = false
        }
        showPageMessage({
          tone: 'success',
          title: 'Vehicle deleted',
          message: `${vehicle.id} was deleted successfully.`
        })
      } catch (error) {
        formError.value = error.message || 'Unable to delete vehicle.'
        showPageMessage({
          tone: 'error',
          title: 'Vehicle was not deleted',
          message: formError.value
        })
      } finally {
        loading.value = false
      }
    }
  })
}
</script>

<style scoped src="./vehicles_styles/VehicleManagementContent.css"></style>
