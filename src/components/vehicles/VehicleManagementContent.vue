<template>
  <div class="vehicle-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Vehicle Management</h1>
        <p class="section-subtitle">Track, assign, and maintain your fleet in one place</p>
      </div>
      <button class="primary-button" type="button" @click="openAdd">
        <v-icon icon="mdi-truck-plus" size="18" />
        Add Vehicle
      </button>
    </div>

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
        </div>

        <div class="toolbar-filter">
          <v-icon icon="mdi-filter-variant" />
          <select v-model="statusFilter">
            <option value="All">All Status</option>
            <option value="Active">Active</option>
            <option value="Maintenance">Maintenance</option>
            <option value="Inactive">Inactive</option>
          </select>
        </div>
      </div>

      <div class="toolbar-count text-muted">
        Showing {{ filteredVehicles.length }} of {{ vehicles.length }} vehicles
      </div>
    </div>

    <div class="card-surface">
      <div class="table-wrap">
        <table class="table-base">
          <thead>
            <tr>
              <th>Vehicle</th>
              <th>Plate Number</th>
              <th>Type</th>
              <th>Status</th>
              <th>Driver Assigned</th>
              <th>Acquired Date</th>
              <th class="align-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="vehicle in filteredVehicles" :key="vehicle.id">
              <td>
                <div class="vehicle-cell">
                  <button
                    class="thumb-button tooltip"
                    type="button"
                    @click="openImage(vehicle.image, vehicle.type)"
                  >
                    <img :src="vehicle.image" :alt="vehicle.type" class="vehicle-image" />
                    <span class="tooltip-text">View vehicle image</span>
                  </button>
                  <div>
                    <strong>{{ vehicle.id }}</strong>
                    <div class="text-muted vehicle-sub">{{ vehicle.model }}</div>
                  </div>
                </div>
              </td>
              <td class="text-muted">{{ vehicle.plate }}</td>
              <td>{{ vehicle.type }}</td>
              <td>
                <span class="badge" :class="statusClass(vehicle.status)">
                  {{ vehicle.status }}
                </span>
              </td>
              <td>
                <div class="driver-cell">
                  <button
                    class="thumb-button tooltip"
                    type="button"
                    @click="openImage(vehicle.driverImage, vehicle.driver)"
                  >
                    <img :src="vehicle.driverImage" :alt="vehicle.driver" class="driver-photo" />
                    <span class="tooltip-text">View driver image</span>
                  </button>
                  <span>{{ vehicle.driver }}</span>
                </div>
              </td>
              <td class="text-muted">{{ formatDate(vehicle.acquiredDate) }}</td>
              <td class="align-right">
                <div class="inline-actions">
                  <button class="icon-button tooltip" type="button" @click="openEdit(vehicle)">
                    <v-icon icon="mdi-pencil-outline" size="18" />
                    <span class="tooltip-text">Edit vehicle</span>
                  </button>
                  <button class="icon-button tooltip" type="button" @click="openDetails(vehicle)">
                    <v-icon icon="mdi-eye-outline" size="18" />
                    <span class="tooltip-text">View details</span>
                  </button>
                  <button
                    class="icon-button tooltip"
                    :class="vehicle.status === 'Active' ? 'warn' : 'good'"
                    type="button"
                    @click="toggleStatus(vehicle.id)"
                  >
                    <v-icon icon="mdi-power" size="18" />
                    <span class="tooltip-text">
                      {{ vehicle.status === 'Active' ? 'Set inactive' : 'Set active' }}
                    </span>
                  </button>
                  <button class="icon-button danger tooltip" type="button" @click="deleteVehicle(vehicle.id)">
                    <v-icon icon="mdi-trash-can-outline" size="18" />
                    <span class="tooltip-text">Delete vehicle</span>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-if="filteredVehicles.length === 0" class="empty-state">
        No vehicles found matching your criteria
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
            <h4>Compliance</h4>
            <div class="details-row"><span>Registration Expiry</span><strong>{{ formatDate(selectedVehicle.registrationExpiry) }}</strong></div>
            <div class="details-row"><span>Road Tax Expiry</span><strong>{{ formatDate(selectedVehicle.roadTaxExpiry) }}</strong></div>
            <div class="details-row"><span>Insurance Expiry</span><strong>{{ formatDate(selectedVehicle.insuranceExpiry) }}</strong></div>
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

        <div class="form-grid">
          <div class="form-field">
            <label>Plate Number</label>
            <input v-model="formData.plate" type="text" placeholder="e.g., YGN-7742" />
          </div>
          <div class="form-field">
            <label>Region</label>
            <input v-model="formData.region" type="text" placeholder="e.g., Yangon" />
          </div>
          <div class="form-field">
            <label>Vehicle Type</label>
            <input v-model="formData.type" type="text" placeholder="e.g., Box Truck" />
          </div>
          <div class="form-field">
            <label>Model</label>
            <input v-model="formData.model" type="text" placeholder="e.g., Isuzu FVR" />
          </div>
          <div class="form-field">
            <label>Status</label>
            <select v-model="formData.status">
              <option value="Active">Active</option>
              <option value="Maintenance">Maintenance</option>
              <option value="Inactive">Inactive</option>
            </select>
          </div>
          <div class="form-field">
            <label>Driver</label>
            <input v-model="formData.driver" type="text" placeholder="Driver name" />
          </div>
          <div class="form-field">
            <label>
              Depot
              <span class="hint tooltip icon-tooltip" tabindex="0" aria-label="Home base or yard">
                <v-icon icon="mdi-information-outline" size="14" />
                <span class="tooltip-text">Home base / yard</span>
              </span>
            </label>
            <input v-model="formData.depot" type="text" placeholder="Depot / yard" />
          </div>
          <div class="form-field">
            <label>Capacity</label>
            <input v-model="formData.capacity" type="text" placeholder="e.g., 6 tons" />
          </div>
          <div class="form-field">
            <label>Fuel Type</label>
            <input v-model="formData.fuelType" type="text" placeholder="e.g., Diesel" />
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
          <div class="form-field">
            <label>Vehicle Image URL</label>
            <input v-model="formData.image" type="url" placeholder="https://..." />
          </div>
          <div class="form-field">
            <label>Driver Image URL</label>
            <input v-model="formData.driverImage" type="url" placeholder="https://..." />
          </div>
        </div>

        <div class="form-actions">
          <button class="ghost-button" type="button" @click="closeForm">Cancel</button>
          <button class="primary-button" type="button" @click="saveForm">
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
import { computed, ref } from 'vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'

const vehicles = ref([
  {
    id: 'VH-2048',
    plate: 'BRC-4521',
    region: 'Bago',
    type: 'Box Truck',
    model: 'Volvo FL 280',
    status: 'Active',
    driver: 'Sarah Johnson',
    driverImage: 'https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=crop&w=800&q=80',
    depot: 'Yangon East Yard',
    capacity: '6 tons',
    fuelType: 'Diesel',
    vin: 'MMTFL280X7A2048',
    engineNo: 'ENG-2048-XY',
    odometer: '182,450 km',
    lastService: '2025-11-10',
    nextService: '2026-04-10',
    serviceNote: 'Brake pads replaced',
    registrationExpiry: '2026-09-30',
    roadTaxExpiry: '2026-06-30',
    insuranceExpiry: '2026-08-15',
    inspectionDue: '2026-05-20',
    acquiredDate: '2017-06-14',
    image: 'https://images.unsplash.com/photo-1489515217757-5fd1be406fef?auto=format&fit=crop&w=1200&q=80'
  },
  {
    id: 'VH-3054',
    plate: 'MDY-1109',
    region: 'Mandalay',
    type: 'Cargo Van',
    model: 'Ford Transit',
    status: 'Maintenance',
    driver: 'Michael Chen',
    driverImage: 'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=800&q=80',
    depot: 'Mandalay Hub',
    capacity: '2 tons',
    fuelType: 'Diesel',
    vin: 'MMTRNS3054F1109',
    engineNo: 'ENG-3054-AK',
    odometer: '96,880 km',
    lastService: '2026-01-06',
    nextService: '2026-03-22',
    serviceNote: 'Transmission inspection',
    registrationExpiry: '2026-10-12',
    roadTaxExpiry: '2026-07-31',
    insuranceExpiry: '2026-09-02',
    inspectionDue: '2026-04-18',
    acquiredDate: '2019-03-22',
    image: 'https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80'
  },
  {
    id: 'VH-1987',
    plate: 'YGN-7742',
    region: 'Yangon',
    type: 'Reefer Truck',
    model: 'Isuzu FVR',
    status: 'Active',
    driver: 'Emily Davis',
    driverImage: 'https://images.unsplash.com/photo-1544005313-94ddf0286df2?auto=format&fit=crop&w=800&q=80',
    depot: 'Thanlyin Cold Chain',
    capacity: '8 tons',
    fuelType: 'Diesel',
    vin: 'MMFVR1987YGN7742',
    engineNo: 'ENG-1987-FR',
    odometer: '143,220 km',
    lastService: '2025-12-02',
    nextService: '2026-04-25',
    serviceNote: 'Reefer unit serviced',
    registrationExpiry: '2026-08-05',
    roadTaxExpiry: '2026-06-10',
    insuranceExpiry: '2026-07-19',
    inspectionDue: '2026-05-02',
    acquiredDate: '2018-11-08',
    image: 'https://images.unsplash.com/photo-1517940310602-26535839fe84?auto=format&fit=crop&w=1200&q=80'
  },
  {
    id: 'VH-4129',
    plate: 'NPT-2306',
    region: 'Naypyitaw',
    type: 'Flatbed',
    model: 'Hino 500',
    status: 'Inactive',
    driver: 'Robert Wilson',
    driverImage: 'https://images.unsplash.com/photo-1506794778202-cad84cf45f1d?auto=format&fit=crop&w=800&q=80',
    depot: 'Naypyitaw Central',
    capacity: '10 tons',
    fuelType: 'Diesel',
    vin: 'MMHINO4129NPT2306',
    engineNo: 'ENG-4129-HN',
    odometer: '210,540 km',
    lastService: '2025-09-18',
    nextService: '2026-02-28',
    serviceNote: 'Awaiting tire replacement',
    registrationExpiry: '2026-04-20',
    roadTaxExpiry: '2026-03-31',
    insuranceExpiry: '2026-05-14',
    inspectionDue: '2026-03-20',
    acquiredDate: '2016-02-17',
    image: 'https://images.unsplash.com/photo-1513735717081-8ad5c3c244eb?auto=format&fit=crop&w=1200&q=80'
  },
  {
    id: 'VH-2661',
    plate: 'BGO-5584',
    region: 'Bago',
    type: 'Delivery Van',
    model: 'Mercedes Sprinter',
    status: 'Active',
    driver: 'John Martinez',
    driverImage: 'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?auto=format&fit=crop&w=800&q=80',
    depot: 'Bago Cross-Dock',
    capacity: '1.5 tons',
    fuelType: 'Diesel',
    vin: 'MMSPR2661BGO5584',
    engineNo: 'ENG-2661-MS',
    odometer: '78,930 km',
    lastService: '2026-02-01',
    nextService: '2026-06-01',
    serviceNote: 'Oil + filter changed',
    registrationExpiry: '2027-01-11',
    roadTaxExpiry: '2026-11-30',
    insuranceExpiry: '2026-12-19',
    inspectionDue: '2026-09-10',
    acquiredDate: '2020-09-30',
    image: 'https://images.unsplash.com/photo-1511919884226-fd3cad34687c?auto=format&fit=crop&w=1200&q=80'
  },
  {
    id: 'VH-3775',
    plate: 'MND-9021',
    region: 'Mandalay',
    type: 'Tanker',
    model: 'Kenworth T800',
    status: 'Maintenance',
    driver: 'Amanda Taylor',
    driverImage: 'https://images.unsplash.com/photo-1544723795-3fb6469f5b39?auto=format&fit=crop&w=800&q=80',
    depot: 'Monywa Depot',
    capacity: '12 tons',
    fuelType: 'Diesel',
    vin: 'MMTNK3775MND9021',
    engineNo: 'ENG-3775-TK',
    odometer: '256,010 km',
    lastService: '2026-01-15',
    nextService: '2026-03-30',
    serviceNote: 'Pump calibration',
    registrationExpiry: '2026-07-02',
    roadTaxExpiry: '2026-06-15',
    insuranceExpiry: '2026-08-07',
    inspectionDue: '2026-04-12',
    acquiredDate: '2017-12-05',
    image: 'https://images.unsplash.com/photo-1517148815978-75f6acaaf32c?auto=format&fit=crop&w=1200&q=80'
  }
])

const searchQuery = ref('')
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
const formData = ref({})

const filteredVehicles = computed(() => {
  const query = searchQuery.value.toLowerCase()
  return vehicles.value.filter((vehicle) => {
    const matchesSearch =
      vehicle.id.toLowerCase().includes(query) ||
      vehicle.plate.toLowerCase().includes(query) ||
      vehicle.driver.toLowerCase().includes(query)
    const matchesStatus = statusFilter.value === 'All' || vehicle.status === statusFilter.value
    return matchesSearch && matchesStatus
  })
})

const activeCount = computed(() => vehicles.value.filter((v) => v.status === 'Active').length)
const maintenanceCount = computed(() => vehicles.value.filter((v) => v.status === 'Maintenance').length)
const inactiveCount = computed(() => vehicles.value.filter((v) => v.status === 'Inactive').length)

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
  status: 'Active',
  driver: '',
  driverImage: '',
  depot: '',
  capacity: '',
  fuelType: '',
  vin: '',
  engineNo: '',
  odometer: '',
  lastService: '',
  nextService: '',
  serviceNote: '',
  registrationExpiry: '',
  roadTaxExpiry: '',
  insuranceExpiry: '',
  inspectionDue: '',
  acquiredDate: '',
  image: ''
})

const openAdd = () => {
  formMode.value = 'add'
  formData.value = buildEmptyForm()
  formError.value = ''
  formOpen.value = true
}

const openEdit = (vehicle) => {
  formMode.value = 'edit'
  formData.value = { ...vehicle }
  formError.value = ''
  formOpen.value = true
}

const closeForm = () => {
  formOpen.value = false
}

const saveForm = () => {
  if (!formData.value.plate || !formData.value.model || !formData.value.driver) {
    formError.value = 'Plate number, model, and driver are required.'
    return
  }

  if (formMode.value === 'add') {
    const newId = `VH-${Math.floor(1000 + Math.random() * 9000)}`
    vehicles.value = [
      {
        ...formData.value,
        id: newId,
        image:
          formData.value.image ||
          'https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=1200&q=80',
        driverImage:
          formData.value.driverImage ||
          'https://images.unsplash.com/photo-1500648767791-00dcc994a43e?auto=format&fit=crop&w=800&q=80'
      },
      ...vehicles.value
    ]
  } else {
    vehicles.value = vehicles.value.map((item) =>
      item.id === formData.value.id ? { ...item, ...formData.value } : item
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

const toggleStatus = (id) => {
  const vehicle = vehicles.value.find((item) => item.id === id)
  if (!vehicle) return
  const nextStatus = vehicle.status === 'Active' ? 'Inactive' : 'Active'
  openConfirm({
    title: `${nextStatus} Vehicle?`,
    message: `This will mark ${vehicle.id} as ${nextStatus.toLowerCase()}.`,
    confirmText: nextStatus,
    tone: 'warning',
    action: () => {
      vehicles.value = vehicles.value.map((item) =>
        item.id === id ? { ...item, status: nextStatus } : item
      )
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
    action: () => {
      vehicles.value = vehicles.value.filter((item) => item.id !== id)
    }
  })
}
</script>

<style scoped>
.vehicle-page {
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

.text-warning {
  color: var(--fleet-warning);
}

.text-danger {
  color: var(--fleet-danger);
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

.toolbar-filter {
  cursor: pointer;
}

.toolbar-search input,
.toolbar-filter select {
  border: none;
  outline: none;
  background: transparent;
  font-size: 14px;
}

.toolbar-filter select {
  cursor: pointer;
}

.toolbar-search {
  flex: 1 1 320px;
  max-width: 520px;
}

.toolbar-search input {
  width: 100%;
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

.table-wrap {
  overflow-x: auto;
}

.align-right {
  text-align: right;
}

.driver-cell {
  display: inline-flex;
  align-items: center;
  gap: 10px;
}

.vehicle-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.vehicle-image {
  width: 54px;
  height: 36px;
  border-radius: 10px;
  object-fit: cover;
  border: 1px solid var(--fleet-border);
}

.vehicle-sub {
  font-size: 12px;
  margin-top: 2px;
}

.driver-photo {
  width: 34px;
  height: 34px;
  border-radius: 50%;
  object-fit: cover;
  border: 1px solid var(--fleet-border);
  display: block;
}

.thumb-button {
  border: none;
  background: transparent;
  padding: 0;
  border-radius: 12px;
  cursor: pointer;
}

.thumb-button:focus-visible {
  outline: 2px solid rgba(37, 99, 235, 0.35);
  outline-offset: 2px;
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

.image-modal {
  padding: 16px 18px 20px;
}

.image-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--fleet-border);
  margin-bottom: 12px;
}

.image-title {
  font-weight: 600;
}

.full-image {
  width: 100%;
  height: 420px;
  border-radius: 14px;
  border: 1px solid var(--fleet-border);
  display: block;
  object-fit: cover;
}

@media (max-width: 720px) {
  .full-image {
    height: 300px;
  }
}

.form-card {
  padding: 18px 20px 22px;
  max-height: 80vh;
  overflow-y: auto;
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
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
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

.hint {
  color: #94a3b8;
  font-size: 11px;
  font-weight: 500;
}

.tooltip {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: help;
}

.icon-tooltip {
  width: 22px;
  height: 22px;
  border-radius: 999px;
  color: #64748b;
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

.inline-actions .tooltip-text {
  left: auto;
  right: 0;
  transform: translateY(6px);
}

.inline-actions .tooltip:hover .tooltip-text,
.inline-actions .tooltip:focus-visible .tooltip-text {
  transform: translateY(0);
}

.thumb-button .tooltip-text {
  left: 0;
  right: auto;
}
</style>
