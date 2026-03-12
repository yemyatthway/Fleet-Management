<template>
  <div class="vehicle-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Vehicle Management</h1>
        <p class="section-subtitle">Track, assign, and maintain your fleet in one place</p>
      </div>
      <button class="primary-button" type="button">
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
                  <img :src="vehicle.image" :alt="vehicle.type" class="vehicle-image" />
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
                  <span class="driver-avatar">{{ initials(vehicle.driver) }}</span>
                  <span>{{ vehicle.driver }}</span>
                </div>
              </td>
              <td class="text-muted">{{ formatDate(vehicle.acquiredDate) }}</td>
              <td class="align-right">
                <div class="inline-actions">
                  <button class="icon-button" type="button">
                    <v-icon icon="mdi-pencil-outline" size="18" />
                  </button>
                  <button class="icon-button" type="button" @click="openDetails(vehicle)">
                    <v-icon icon="mdi-eye-outline" size="18" />
                  </button>
                  <button
                    class="icon-button"
                    :class="vehicle.status === 'Active' ? 'warn' : 'good'"
                    type="button"
                    @click="toggleStatus(vehicle.id)"
                  >
                    <v-icon icon="mdi-power" size="18" />
                  </button>
                  <button class="icon-button danger" type="button" @click="deleteVehicle(vehicle.id)">
                    <v-icon icon="mdi-trash-can-outline" size="18" />
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

          <div class="details-section">
            <h4>Tracking</h4>
            <div class="details-row"><span>GPS Device</span><strong>{{ selectedVehicle.gpsId }}</strong></div>
            <div class="details-row"><span>Last Ping</span><strong>{{ selectedVehicle.lastPing }}</strong></div>
            <div class="details-row"><span>Last Location</span><strong>{{ selectedVehicle.lastLocation }}</strong></div>
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

const vehicles = ref([
  {
    id: 'VH-2048',
    plate: 'BRC-4521',
    region: 'Bago',
    type: 'Box Truck',
    model: 'Volvo FL 280',
    status: 'Active',
    driver: 'Sarah Johnson',
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
    gpsId: 'GPS-00912',
    lastPing: '5 min ago',
    lastLocation: 'Hlegu, Yangon',
    acquiredDate: '2017-06-14',
    image: 'https://images.unsplash.com/photo-1489515217757-5fd1be406fef?auto=format&fit=crop&w=160&q=80'
  },
  {
    id: 'VH-3054',
    plate: 'MDY-1109',
    region: 'Mandalay',
    type: 'Cargo Van',
    model: 'Ford Transit',
    status: 'Maintenance',
    driver: 'Michael Chen',
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
    gpsId: 'GPS-01433',
    lastPing: '12 min ago',
    lastLocation: 'Amarapura, Mandalay',
    acquiredDate: '2019-03-22',
    image: 'https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&w=160&q=80'
  },
  {
    id: 'VH-1987',
    plate: 'YGN-7742',
    region: 'Yangon',
    type: 'Reefer Truck',
    model: 'Isuzu FVR',
    status: 'Active',
    driver: 'Emily Davis',
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
    gpsId: 'GPS-01788',
    lastPing: '2 min ago',
    lastLocation: 'Hlaing, Yangon',
    acquiredDate: '2018-11-08',
    image: 'https://images.unsplash.com/photo-1517940310602-26535839fe84?auto=format&fit=crop&w=160&q=80'
  },
  {
    id: 'VH-4129',
    plate: 'NPT-2306',
    region: 'Naypyitaw',
    type: 'Flatbed',
    model: 'Hino 500',
    status: 'Inactive',
    driver: 'Robert Wilson',
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
    gpsId: 'GPS-02005',
    lastPing: '2 days ago',
    lastLocation: 'Lewe, Naypyitaw',
    acquiredDate: '2016-02-17',
    image: 'https://images.unsplash.com/photo-1513735717081-8ad5c3c244eb?auto=format&fit=crop&w=160&q=80'
  },
  {
    id: 'VH-2661',
    plate: 'BGO-5584',
    region: 'Bago',
    type: 'Delivery Van',
    model: 'Mercedes Sprinter',
    status: 'Active',
    driver: 'John Martinez',
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
    gpsId: 'GPS-02311',
    lastPing: '8 min ago',
    lastLocation: 'Bago City',
    acquiredDate: '2020-09-30',
    image: 'https://images.unsplash.com/photo-1511919884226-fd3cad34687c?auto=format&fit=crop&w=160&q=80'
  },
  {
    id: 'VH-3775',
    plate: 'MND-9021',
    region: 'Mandalay',
    type: 'Tanker',
    model: 'Kenworth T800',
    status: 'Maintenance',
    driver: 'Amanda Taylor',
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
    gpsId: 'GPS-02645',
    lastPing: '28 min ago',
    lastLocation: 'Monywa',
    acquiredDate: '2017-12-05',
    image: 'https://images.unsplash.com/photo-1517148815978-75f6acaaf32c?auto=format&fit=crop&w=160&q=80'
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

const initials = (name) => name.split(' ').map((part) => part[0]).join('')

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

.driver-avatar {
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
</style>
