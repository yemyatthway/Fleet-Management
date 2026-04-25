<template>
  <div class="trips-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Trips Management</h1>
        <p class="section-subtitle">
          Plan dispatches, monitor live operations, and capture the operational
          fields teams use in the field.
        </p>
      </div>
      <button class="primary-button" type="button" @click="openAddDialog">
        <v-icon icon="mdi-plus" size="18" />
        Create Trip
      </button>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Trips</p>
        <h3>{{ trips.length }}</h3>
        <span class="stat-foot text-muted"
          >Scheduled, active, and completed runs</span
        >
      </div>
      <div class="stat-card">
        <p>In Transit</p>
        <h3 class="text-info">{{ inTransitCount }}</h3>
        <span class="stat-foot text-muted">Trips currently on the road</span>
      </div>
      <div class="stat-card">
        <p>Delayed</p>
        <h3 class="text-warning">{{ delayedCount }}</h3>
        <span class="stat-foot text-muted">Need dispatcher follow-up</span>
      </div>
      <div class="stat-card">
        <p>Completed</p>
        <h3 class="text-success">{{ completedCount }}</h3>
        <span class="stat-foot text-muted">Delivered and closed</span>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input
            v-model.trim="searchQuery"
            type="text"
            placeholder="Search trip, route, driver, customer, or vehicle..."
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
              <option
                v-for="status in tripStatuses"
                :key="status"
                :value="status"
              >
                {{ status }}
              </option>
            </select>
          </div>

          <div class="toolbar-filter">
            <v-icon icon="mdi-truck-delivery-outline" />
            <select v-model="tripTypeFilter">
              <option value="All">All Types</option>
              <option v-for="type in tripTypes" :key="type" :value="type">
                {{ type }}
              </option>
            </select>
          </div>
        </div>
      </div>

      <div class="toolbar-count text-muted">
        Showing {{ filteredTrips.length }} of {{ trips.length }} trips
      </div>
    </div>

    <div class="card-surface table-card">
      <div class="table-wrap">
        <v-data-table
          class="table-base trips-table"
          :headers="tripHeaders"
          :items="filteredTrips"
          :page="tablePage"
          :items-per-page="itemsPerPage"
          :items-per-page-options="[8, 16, 24]"
          :mobile-breakpoint="0"
          :mobile="false"
          fixed-header
          height="560"
          density="comfortable"
          @update:options="handleTableOptions"
        >
          <template #item.displayId="{ index }">
            <span class="row-id">{{ rowNumber(index) }}</span>
          </template>

          <template #item.tripNumber="{ item }">
            <div class="trip-id-cell">
              <strong class="trip-number">{{ item.tripNumber }}</strong>
              <div class="text-muted trip-sub">
                {{ item.tripType }} • {{ item.priority }}
              </div>
            </div>
          </template>

          <template #item.schedule="{ item }">
            <div class="stack-cell">
              <strong class="schedule-primary">{{ formatDateTime(item.departureDateTime) }}</strong>
              <span class="text-muted"
                >ETA {{ formatDateTime(item.estimatedArrival) }}</span
              >
            </div>
          </template>

          <template #item.route="{ item }">
            <div class="route-cell">
              <div class="route-line">
                <v-icon icon="mdi-map-marker-radius-outline" size="18" />
                <span>{{ item.pickupLocation }}</span>
              </div>
              <div class="route-arrow text-muted">
                to {{ item.dropoffLocation }}
              </div>
            </div>
          </template>

          <template #item.vehicle="{ item }">
            <div class="stack-cell">
              <strong class="vehicle-plate">{{ item.vehiclePlate }}</strong>
              <span class="text-muted">
                {{ item.vehicleId
                }}<span v-if="item.trailerNumber">
                  • {{ item.trailerNumber }}</span
                >
              </span>
            </div>
          </template>

          <template #item.driver="{ item }">
            <div class="stack-cell">
              <strong>{{ item.driverName }}</strong>
              <span class="text-muted">{{ item.dispatcherName }}</span>
            </div>
          </template>

          <template #item.load="{ item }">
            <div class="stack-cell">
              <strong>{{ item.cargoType }}</strong>
              <span class="text-muted">
                {{ item.loadWeightKg.toLocaleString() }} kg •
                {{ item.plannedDistanceKm }} km
              </span>
            </div>
          </template>

          <template #item.status="{ item }">
            <span class="badge" :class="statusClass(item.status)">
              {{ item.status }}
            </span>
          </template>

          <template #item.actions="{ item }">
            <div class="inline-actions">
              <button
                class="icon-button tooltip"
                type="button"
                @click="openDetails(item)"
              >
                <v-icon icon="mdi-eye-outline" size="18" />
                <span class="tooltip-text">View details</span>
              </button>
              <button
                class="icon-button tooltip"
                type="button"
                @click="openEditDialog(item)"
              >
                <v-icon icon="mdi-pencil-outline" size="18" />
                <span class="tooltip-text">Edit trip</span>
              </button>
              <button
                class="icon-button danger tooltip"
                type="button"
                @click="deleteTrip(item.id)"
              >
                <v-icon icon="mdi-trash-can-outline" size="18" />
                <span class="tooltip-text">Delete trip</span>
              </button>
            </div>
          </template>

          <template #no-data>
            <div class="empty-state">No trips found matching your filters</div>
          </template>
        </v-data-table>
      </div>
    </div>

    <v-dialog v-model="detailsOpen" max-width="1100">
      <div v-if="selectedTrip" class="card-surface details-card">
        <div class="details-header">
          <div>
            <div class="details-title">
              {{ selectedTrip.tripNumber }} • {{ selectedTrip.customerName }}
            </div>
            <div class="details-subtitle text-muted">
              {{ selectedTrip.tripType }} • {{ selectedTrip.status }} •
              {{ selectedTrip.priority }}
            </div>
          </div>
          <button
            class="icon-button"
            type="button"
            @click="detailsOpen = false"
          >
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>

        <div class="details-grid">
          <div class="details-section">
            <h4>Dispatch</h4>
            <div class="details-row">
              <span>Vehicle</span
              ><strong
                >{{ selectedTrip.vehiclePlate }} ({{
                  selectedTrip.vehicleId
                }})</strong
              >
            </div>
            <div class="details-row">
              <span>Driver</span><strong>{{ selectedTrip.driverName }}</strong>
            </div>
            <div class="details-row">
              <span>Co-driver</span
              ><strong>{{ selectedTrip.coDriverName || "—" }}</strong>
            </div>
            <div class="details-row">
              <span>Dispatcher</span
              ><strong>{{ selectedTrip.dispatcherName }}</strong>
            </div>
            <div class="details-row">
              <span>Department</span
              ><strong>{{ selectedTrip.department }}</strong>
            </div>
            <div class="details-row">
              <span>Cost Center</span
              ><strong>{{ selectedTrip.costCenter || "—" }}</strong>
            </div>
          </div>

          <div class="details-section">
            <h4>Route & Schedule</h4>
            <div class="details-row">
              <span>Pickup</span
              ><strong>{{ selectedTrip.pickupLocation }}</strong>
            </div>
            <div class="details-row">
              <span>Dropoff</span
              ><strong>{{ selectedTrip.dropoffLocation }}</strong>
            </div>
            <div class="details-row">
              <span>Departure</span
              ><strong>{{
                formatDateTime(selectedTrip.departureDateTime)
              }}</strong>
            </div>
            <div class="details-row">
              <span>ETA</span
              ><strong>{{
                formatDateTime(selectedTrip.estimatedArrival)
              }}</strong>
            </div>
            <div class="details-row">
              <span>Arrival</span
              ><strong>{{ formatDateTime(selectedTrip.actualArrival) }}</strong>
            </div>
            <div class="details-row">
              <span>Distance</span
              ><strong>{{ selectedTrip.plannedDistanceKm }} km</strong>
            </div>
          </div>

          <div class="details-section">
            <h4>Load & Service</h4>
            <div class="details-row">
              <span>Customer</span
              ><strong>{{ selectedTrip.customerName }}</strong>
            </div>
            <div class="details-row">
              <span>Cargo Type</span
              ><strong>{{ selectedTrip.cargoType }}</strong>
            </div>
            <div class="details-row">
              <span>Weight</span
              ><strong
                >{{ selectedTrip.loadWeightKg.toLocaleString() }} kg</strong
              >
            </div>
            <div class="details-row">
              <span>Volume</span
              ><strong>{{ selectedTrip.loadVolumeM3 }} m3</strong>
            </div>
            <div class="details-row">
              <span>Pickup Contact</span
              ><strong>{{ selectedTrip.pickupContact || "—" }}</strong>
            </div>
            <div class="details-row">
              <span>Dropoff Contact</span
              ><strong>{{ selectedTrip.dropoffContact || "—" }}</strong>
            </div>
          </div>

          <div class="details-section">
            <h4>Fuel & Odometer</h4>
            <div class="details-row">
              <span>Start Odometer</span
              ><strong
                >{{
                  selectedTrip.startingOdometerKm.toLocaleString()
                }}
                km</strong
              >
            </div>
            <div class="details-row">
              <span>Current Odometer</span
              ><strong
                >{{
                  selectedTrip.currentOdometerKm.toLocaleString()
                }}
                km</strong
              >
            </div>
            <div class="details-row">
              <span>End Odometer</span
              ><strong>{{
                formatNumber(selectedTrip.endingOdometerKm, " km")
              }}</strong>
            </div>
            <div class="details-row">
              <span>Fuel Issued</span
              ><strong>{{ selectedTrip.fuelIssuedLiters }} L</strong>
            </div>
            <div class="details-row">
              <span>Toll Estimate</span
              ><strong>{{ formatCurrency(selectedTrip.tollEstimate) }}</strong>
            </div>
            <div class="details-row">
              <span>Temperature Controlled</span
              ><strong>{{
                selectedTrip.temperatureControlled ? "Yes" : "No"
              }}</strong>
            </div>
          </div>

          <div class="details-section full-width">
            <h4>Operational Notes</h4>
            <div class="notes-grid">
              <div class="note-card">
                <span class="note-label">Special Instructions</span>
                <p>{{ selectedTrip.specialInstructions || "—" }}</p>
              </div>
              <div class="note-card">
                <span class="note-label">Driver Notes</span>
                <p>{{ selectedTrip.driverNotes || "—" }}</p>
              </div>
              <div class="note-card">
                <span class="note-label">Compliance</span>
                <p>
                  {{
                    selectedTrip.permitRequired
                      ? "Permit required"
                      : "No permit required"
                  }}
                </p>
                <p v-if="selectedTrip.temperatureControlled" class="text-muted">
                  Temperature range:
                  {{ selectedTrip.temperatureRange || "Not set" }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </v-dialog>

    <v-dialog v-model="formOpen" max-width="1160">
      <div class="card-surface form-card">
        <div class="form-header">
          <div>
            <div class="form-title">
              {{ formMode === "edit" ? "Edit Trip" : "Create Trip" }}
            </div>
            <div class="text-muted">
              Frontend-only first pass for real dispatch operations.
            </div>
          </div>
          <button class="icon-button" type="button" @click="closeForm">
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>

        <div v-if="formError" class="form-error">{{ formError }}</div>

        <form class="form-layout" @submit.prevent="submitForm">
          <div class="form-scroll">
            <div class="form-section">
              <div class="section-heading">Trip Basics</div>
              <div class="form-grid">
              <div class="field">
                <label class="required">Trip Number</label>
                <input
                  v-model.trim="form.tripNumber"
                  type="text"
                  placeholder="TRP-3101"
                  required
                />
              </div>
              <div class="field">
                <label class="required">Trip Type</label>
                <select v-model="form.tripType" required>
                  <option v-for="type in tripTypes" :key="type" :value="type">
                    {{ type }}
                  </option>
                </select>
              </div>
              <div class="field">
                <label class="required">Status</label>
                <select v-model="form.status" required>
                  <option
                    v-for="status in tripStatuses"
                    :key="status"
                    :value="status"
                  >
                    {{ status }}
                  </option>
                </select>
              </div>
              <div class="field">
                <label class="required">Priority</label>
                <select v-model="form.priority" required>
                  <option
                    v-for="priority in priorities"
                    :key="priority"
                    :value="priority"
                  >
                    {{ priority }}
                  </option>
                </select>
              </div>
              <div class="field">
                <label class="required">Customer</label>
                <input
                  v-model.trim="form.customerName"
                  type="text"
                  placeholder="Metro Retail Group"
                  required
                />
              </div>
              <div class="field">
                <label class="required">Department</label>
                <input
                  v-model.trim="form.department"
                  type="text"
                  placeholder="Distribution"
                  required
                />
              </div>
              <div class="field">
                <label>Cost Center</label>
                <input
                  v-model.trim="form.costCenter"
                  type="text"
                  placeholder="OPS-NORTH-01"
                />
              </div>
              <div class="field">
                <label>Trailer Number</label>
                <input
                  v-model.trim="form.trailerNumber"
                  type="text"
                  placeholder="TRL-102"
                />
              </div>
            </div>
            </div>

            <div class="form-section">
              <div class="section-heading">Dispatch Assignment</div>
              <div class="form-grid">
              <div class="field">
                <label class="required">Vehicle ID</label>
                <input
                  v-model.trim="form.vehicleId"
                  type="text"
                  placeholder="FL-2218"
                  required
                />
              </div>
              <div class="field">
                <label class="required">Vehicle Plate</label>
                <input
                  v-model.trim="form.vehiclePlate"
                  type="text"
                  placeholder="YGN-8K/2218"
                  required
                />
              </div>
              <div class="field">
                <label class="required">Driver Name</label>
                <input
                  v-model.trim="form.driverName"
                  type="text"
                  placeholder="Aung Kyaw Min"
                  required
                />
              </div>
              <div class="field">
                <label>Co-driver Name</label>
                <input
                  v-model.trim="form.coDriverName"
                  type="text"
                  placeholder="Optional co-driver"
                />
              </div>
              <div class="field">
                <label class="required">Dispatcher</label>
                <input
                  v-model.trim="form.dispatcherName"
                  type="text"
                  placeholder="Nilar Htun"
                  required
                />
              </div>
              <div class="field">
                <label class="required">Cargo Type</label>
                <input
                  v-model.trim="form.cargoType"
                  type="text"
                  placeholder="Cold chain produce"
                  required
                />
              </div>
              <div class="field">
                <label class="required">Load Weight (kg)</label>
                <input
                  v-model.number="form.loadWeightKg"
                  type="number"
                  min="0"
                  placeholder="18000"
                  required
                />
              </div>
              <div class="field">
                <label>Load Volume (m3)</label>
                <input
                  v-model.number="form.loadVolumeM3"
                  type="number"
                  min="0"
                  step="0.1"
                  placeholder="42.5"
                />
              </div>
            </div>
            </div>

            <div class="form-section">
              <div class="section-heading">Route, Timing, and Contacts</div>
              <div class="form-grid">
              <div class="field">
                <label class="required">Pickup Location</label>
                <input
                  v-model.trim="form.pickupLocation"
                  type="text"
                  placeholder="Yangon Distribution Hub"
                  required
                />
              </div>
              <div class="field">
                <label class="required">Dropoff Location</label>
                <input
                  v-model.trim="form.dropoffLocation"
                  type="text"
                  placeholder="Mandalay Regional DC"
                  required
                />
              </div>
              <div class="field">
                <label>Pickup Contact</label>
                <input
                  v-model.trim="form.pickupContact"
                  type="text"
                  placeholder="Ko Min • +95 9 000 000000"
                />
              </div>
              <div class="field">
                <label>Dropoff Contact</label>
                <input
                  v-model.trim="form.dropoffContact"
                  type="text"
                  placeholder="Ma Ei • +95 9 111 111111"
                />
              </div>
              <div class="field">
                <label class="required">Departure</label>
                <input
                  v-model="form.departureDateTime"
                  type="datetime-local"
                  required
                />
              </div>
              <div class="field">
                <label class="required">Estimated Arrival</label>
                <input
                  v-model="form.estimatedArrival"
                  type="datetime-local"
                  required
                />
              </div>
              <div class="field">
                <label>Actual Arrival</label>
                <input v-model="form.actualArrival" type="datetime-local" />
              </div>
              <div class="field">
                <label class="required">Planned Distance (km)</label>
                <input
                  v-model.number="form.plannedDistanceKm"
                  type="number"
                  min="0"
                  placeholder="622"
                  required
                />
              </div>
            </div>
            </div>

            <div class="form-section">
              <div class="section-heading">Fuel, Odometer, and Compliance</div>
              <div class="form-grid">
              <div class="field">
                <label class="required">Start Odometer (km)</label>
                <input
                  v-model.number="form.startingOdometerKm"
                  type="number"
                  min="0"
                  placeholder="129500"
                  required
                />
              </div>
              <div class="field">
                <label>Current Odometer (km)</label>
                <input
                  v-model.number="form.currentOdometerKm"
                  type="number"
                  min="0"
                  placeholder="130120"
                />
              </div>
              <div class="field">
                <label>End Odometer (km)</label>
                <input
                  v-model.number="form.endingOdometerKm"
                  type="number"
                  min="0"
                  placeholder="130156"
                />
              </div>
              <div class="field">
                <label>Fuel Issued (L)</label>
                <input
                  v-model.number="form.fuelIssuedLiters"
                  type="number"
                  min="0"
                  step="0.1"
                  placeholder="195"
                />
              </div>
              <div class="field">
                <label>Toll Estimate</label>
                <input
                  v-model.number="form.tollEstimate"
                  type="number"
                  min="0"
                  step="0.01"
                  placeholder="85000"
                />
              </div>
              <div class="field">
                <label>Temperature Range</label>
                <input
                  v-model.trim="form.temperatureRange"
                  type="text"
                  placeholder="2C to 8C"
                />
              </div>
              <div class="field checkbox-field">
                <label>
                  <input v-model="form.permitRequired" type="checkbox" />
                  Permit required
                </label>
              </div>
              <div class="field checkbox-field">
                <label>
                  <input v-model="form.temperatureControlled" type="checkbox" />
                  Temperature controlled
                </label>
              </div>
            </div>
            </div>

            <div class="form-section">
              <div class="section-heading">Instructions and Notes</div>
              <div class="form-grid notes-form-grid">
              <div class="field full">
                <label>Special Instructions</label>
                <textarea
                  v-model.trim="form.specialInstructions"
                  rows="3"
                  placeholder="Dock appointment, gate pass, unloading conditions..."
                />
              </div>
              <div class="field full">
                <label>Driver Notes</label>
                <textarea
                  v-model.trim="form.driverNotes"
                  rows="3"
                  placeholder="Traffic, incident, waiting time, or field notes..."
                />
              </div>
            </div>
          </div>
          </div>

          <div class="form-actions">
            <button class="ghost-button" type="button" @click="closeForm">
              Cancel
            </button>
            <button class="primary-button" type="submit">
              {{ formMode === "edit" ? "Save Changes" : "Create Trip" }}
            </button>
          </div>
        </form>
      </div>
    </v-dialog>
  </div>
</template>

<script setup>
import { computed, reactive, ref } from "vue";

const tripStatuses = [
  "Scheduled",
  "In Transit",
  "Delayed",
  "Completed",
  "Cancelled",
];
const tripTypes = [
  "Delivery",
  "Pickup",
  "Transfer",
  "Return Load",
  "Maintenance Run",
];
const priorities = ["Low", "Normal", "High", "Critical"];

const tripHeaders = [
  { title: "No.", key: "displayId", sortable: false },
  { title: "Trip", key: "tripNumber", sortable: false },
  { title: "Schedule", key: "schedule", sortable: false },
  { title: "Route", key: "route", sortable: false },
  { title: "Vehicle", key: "vehicle", sortable: false },
  { title: "Driver", key: "driver", sortable: false },
  { title: "Load", key: "load", sortable: false },
  { title: "Status", key: "status", sortable: false },
  {
    title: "Actions",
    key: "actions",
    sortable: false,
    align: "end",
    width: 140,
  },
];

const createEmptyTrip = () => ({
  id: null,
  tripNumber: "",
  tripType: "Delivery",
  status: "Scheduled",
  priority: "Normal",
  customerName: "",
  department: "",
  costCenter: "",
  vehicleId: "",
  vehiclePlate: "",
  trailerNumber: "",
  driverName: "",
  coDriverName: "",
  dispatcherName: "",
  cargoType: "",
  loadWeightKg: 0,
  loadVolumeM3: 0,
  pickupLocation: "",
  dropoffLocation: "",
  pickupContact: "",
  dropoffContact: "",
  departureDateTime: "",
  estimatedArrival: "",
  actualArrival: "",
  plannedDistanceKm: 0,
  startingOdometerKm: 0,
  currentOdometerKm: 0,
  endingOdometerKm: null,
  fuelIssuedLiters: 0,
  tollEstimate: 0,
  permitRequired: false,
  temperatureControlled: false,
  temperatureRange: "",
  specialInstructions: "",
  driverNotes: "",
});

const trips = ref([
  {
    id: 1,
    tripNumber: "TRP-3101",
    tripType: "Delivery",
    status: "In Transit",
    priority: "High",
    customerName: "Metro Retail Group",
    department: "Distribution",
    costCenter: "OPS-YGN-01",
    vehicleId: "FL-2218",
    vehiclePlate: "YGN-8K/2218",
    trailerNumber: "TRL-104",
    driverName: "Aung Kyaw Min",
    coDriverName: "Ye Lin Aung",
    dispatcherName: "Nilar Htun",
    cargoType: "Chilled beverages",
    loadWeightKg: 18200,
    loadVolumeM3: 39.4,
    pickupLocation: "Yangon Cold Chain Hub",
    dropoffLocation: "Mandalay North Retail DC",
    pickupContact: "Ko Min • +95 9 420 111 222",
    dropoffContact: "Ma Ei • +95 9 420 333 444",
    departureDateTime: "2026-04-25T05:30",
    estimatedArrival: "2026-04-25T15:45",
    actualArrival: "",
    plannedDistanceKm: 627,
    startingOdometerKm: 129540,
    currentOdometerKm: 129966,
    endingOdometerKm: null,
    fuelIssuedLiters: 210,
    tollEstimate: 98000,
    permitRequired: true,
    temperatureControlled: true,
    temperatureRange: "2C to 8C",
    specialInstructions:
      "Cold room unloading slot booked for 15:30. Call 30 minutes before arrival.",
    driverNotes:
      "Minor congestion after Meiktila bypass; ETA still within slot.",
  },
  {
    id: 2,
    tripNumber: "TRP-3102",
    tripType: "Pickup",
    status: "Scheduled",
    priority: "Normal",
    customerName: "Shwe Parts Manufacturing",
    department: "Procurement",
    costCenter: "OPS-BGO-03",
    vehicleId: "FL-1984",
    vehiclePlate: "BGO-5E/1984",
    trailerNumber: "",
    driverName: "Zaw Myint Oo",
    coDriverName: "",
    dispatcherName: "Soe Thiri",
    cargoType: "Spare parts pallets",
    loadWeightKg: 7600,
    loadVolumeM3: 21.8,
    pickupLocation: "Bago Industrial Zone",
    dropoffLocation: "Yangon Central Workshop",
    pickupContact: "U Htet • +95 9 784 102 301",
    dropoffContact: "Workshop Gate • +95 9 784 102 899",
    departureDateTime: "2026-04-25T13:00",
    estimatedArrival: "2026-04-25T17:30",
    actualArrival: "",
    plannedDistanceKm: 92,
    startingOdometerKm: 88410,
    currentOdometerKm: 88410,
    endingOdometerKm: null,
    fuelIssuedLiters: 48,
    tollEstimate: 18000,
    permitRequired: false,
    temperatureControlled: false,
    temperatureRange: "",
    specialInstructions:
      "Forklift only at pickup site. Verify pallet count before sealing.",
    driverNotes: "",
  },
  {
    id: 3,
    tripNumber: "TRP-3103",
    tripType: "Transfer",
    status: "Delayed",
    priority: "Critical",
    customerName: "Internal Fleet Support",
    department: "Operations",
    costCenter: "OPS-MDY-02",
    vehicleId: "FL-2407",
    vehiclePlate: "MDY-3L/2407",
    trailerNumber: "TRL-090",
    driverName: "Moe Set Paing",
    coDriverName: "",
    dispatcherName: "Nilar Htun",
    cargoType: "Generator units",
    loadWeightKg: 13400,
    loadVolumeM3: 31.2,
    pickupLocation: "Mandalay Service Yard",
    dropoffLocation: "Magway Backup Depot",
    pickupContact: "Depot Lead • +95 9 777 120 220",
    dropoffContact: "Magway Ops • +95 9 777 120 990",
    departureDateTime: "2026-04-25T04:45",
    estimatedArrival: "2026-04-25T11:15",
    actualArrival: "",
    plannedDistanceKm: 274,
    startingOdometerKm: 154020,
    currentOdometerKm: 154188,
    endingOdometerKm: null,
    fuelIssuedLiters: 112,
    tollEstimate: 42000,
    permitRequired: true,
    temperatureControlled: false,
    temperatureRange: "",
    specialInstructions:
      "Escort to join at Myingyan junction. Oversized load clearance already approved.",
    driverNotes: "Stopped 45 minutes due to axle inspection at checkpoint.",
  },
  {
    id: 4,
    tripNumber: "TRP-3098",
    tripType: "Return Load",
    status: "Completed",
    priority: "Low",
    customerName: "Ayeyar Logistics",
    department: "Backhaul",
    costCenter: "OPS-YGN-05",
    vehicleId: "FL-1775",
    vehiclePlate: "YGN-2B/1775",
    trailerNumber: "",
    driverName: "Sandar Lin",
    coDriverName: "",
    dispatcherName: "Soe Thiri",
    cargoType: "Packaging materials",
    loadWeightKg: 5200,
    loadVolumeM3: 18.6,
    pickupLocation: "Naypyidaw Supplier Park",
    dropoffLocation: "Yangon Packaging Warehouse",
    pickupContact: "Supplier Desk • +95 9 510 230 190",
    dropoffContact: "Warehouse Dock 4 • +95 9 510 230 290",
    departureDateTime: "2026-04-24T06:15",
    estimatedArrival: "2026-04-24T13:40",
    actualArrival: "2026-04-24T13:18",
    plannedDistanceKm: 371,
    startingOdometerKm: 103400,
    currentOdometerKm: 103772,
    endingOdometerKm: 103772,
    fuelIssuedLiters: 98,
    tollEstimate: 56000,
    permitRequired: false,
    temperatureControlled: false,
    temperatureRange: "",
    specialInstructions:
      "Backhaul approved only after empty pallet return is confirmed.",
    driverNotes: "Delivered ahead of slot. No unloading issue.",
  },
]);

const searchQuery = ref("");
const statusFilter = ref("All");
const tripTypeFilter = ref("All");
const selectedTrip = ref(null);
const detailsOpen = ref(false);
const formOpen = ref(false);
const formMode = ref("create");
const formError = ref("");
const editingTripId = ref(null);
const tablePage = ref(1);
const itemsPerPage = ref(8);
const form = reactive(createEmptyTrip());

const filteredTrips = computed(() => {
  const query = searchQuery.value.toLowerCase();

  return trips.value.filter((trip) => {
    const matchesStatus =
      statusFilter.value === "All" || trip.status === statusFilter.value;
    const matchesType =
      tripTypeFilter.value === "All" || trip.tripType === tripTypeFilter.value;
    const matchesSearch =
      !query ||
      [
        trip.tripNumber,
        trip.pickupLocation,
        trip.dropoffLocation,
        trip.driverName,
        trip.dispatcherName,
        trip.customerName,
        trip.vehicleId,
        trip.vehiclePlate,
        trip.cargoType,
      ].some((value) =>
        String(value || "")
          .toLowerCase()
          .includes(query),
      );

    return matchesStatus && matchesType && matchesSearch;
  });
});

const inTransitCount = computed(
  () => trips.value.filter((trip) => trip.status === "In Transit").length,
);
const delayedCount = computed(
  () => trips.value.filter((trip) => trip.status === "Delayed").length,
);
const completedCount = computed(
  () => trips.value.filter((trip) => trip.status === "Completed").length,
);

const statusClass = (status) => {
  if (status === "Completed") return "success";
  if (status === "In Transit") return "info";
  if (status === "Delayed") return "warning";
  if (status === "Cancelled") return "danger";
  return "neutral";
};

const formatDateTime = (value) => {
  if (!value) return "—";

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;

  return new Intl.DateTimeFormat("en-US", {
    month: "short",
    day: "numeric",
    year: "numeric",
    hour: "numeric",
    minute: "2-digit",
  }).format(date);
};

const formatCurrency = (value) => {
  if (value === null || value === undefined || value === "") return "—";

  return new Intl.NumberFormat("en-US", {
    style: "currency",
    currency: "MMK",
    maximumFractionDigits: 0,
  })
    .format(value)
    .replace("MMK", "MMK ");
};

const formatNumber = (value, suffix = "") => {
  if (value === null || value === undefined || value === "") return "—";
  return `${Number(value).toLocaleString()}${suffix}`;
};

const handleTableOptions = (options) => {
  tablePage.value = Number(options.page) || 1;
  itemsPerPage.value = Number(options.itemsPerPage) || 8;
};

const rowNumber = (index) =>
  (tablePage.value - 1) * itemsPerPage.value + index + 1;

const resetForm = () => {
  Object.assign(form, createEmptyTrip());
  formError.value = "";
  editingTripId.value = null;
};

const openDetails = (trip) => {
  selectedTrip.value = { ...trip };
  detailsOpen.value = true;
};

const openAddDialog = () => {
  formMode.value = "create";
  resetForm();
  form.tripNumber = `TRP-${3100 + trips.value.length + 1}`;
  formOpen.value = true;
};

const openEditDialog = (trip) => {
  formMode.value = "edit";
  resetForm();
  Object.assign(form, { ...trip });
  editingTripId.value = trip.id;
  formOpen.value = true;
};

const closeForm = () => {
  formOpen.value = false;
  resetForm();
};

const submitForm = () => {
  if (
    !form.tripNumber ||
    !form.vehicleId ||
    !form.driverName ||
    !form.pickupLocation ||
    !form.dropoffLocation
  ) {
    formError.value =
      "Fill in the required trip, assignment, and route fields.";
    return;
  }

  if (
    form.estimatedArrival &&
    form.departureDateTime &&
    form.estimatedArrival < form.departureDateTime
  ) {
    formError.value = "Estimated arrival cannot be earlier than departure.";
    return;
  }

  const payload = {
    ...form,
    id: editingTripId.value ?? Date.now(),
    loadWeightKg: Number(form.loadWeightKg || 0),
    loadVolumeM3: Number(form.loadVolumeM3 || 0),
    plannedDistanceKm: Number(form.plannedDistanceKm || 0),
    startingOdometerKm: Number(form.startingOdometerKm || 0),
    currentOdometerKm: Number(form.currentOdometerKm || 0),
    endingOdometerKm:
      form.endingOdometerKm === null || form.endingOdometerKm === ""
        ? null
        : Number(form.endingOdometerKm),
    fuelIssuedLiters: Number(form.fuelIssuedLiters || 0),
    tollEstimate: Number(form.tollEstimate || 0),
  };

  if (formMode.value === "edit" && editingTripId.value !== null) {
    trips.value = trips.value.map((trip) =>
      trip.id === editingTripId.value ? payload : trip,
    );
  } else {
    trips.value = [payload, ...trips.value];
  }

  closeForm();
};

const deleteTrip = (id) => {
  trips.value = trips.value.filter((trip) => trip.id !== id);
  if (selectedTrip.value?.id === id) {
    detailsOpen.value = false;
    selectedTrip.value = null;
  }
};
</script>

<style scoped>
.trips-page {
  display: grid;
  gap: 20px;
  padding: 24px;
}

.page-header,
.toolbar-row,
.toolbar-actions,
.stats-grid,
.details-header,
.form-header,
.form-actions,
.notes-grid {
  display: flex;
}

.page-header,
.toolbar-row,
.details-header,
.form-header,
.form-actions {
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.stats-grid {
  gap: 16px;
  flex-wrap: wrap;
}

.stat-card {
  flex: 1 1 220px;
  background: var(--fleet-surface);
  border: 1px solid var(--fleet-border);
  border-radius: 18px;
  padding: 20px;
}

.stat-card p,
.stat-foot,
.section-heading,
.note-label {
  color: var(--fleet-muted);
}

.stat-card p {
  margin: 0 0 10px;
  font-size: 14px;
  font-weight: 600;
}

.stat-card h3 {
  margin: 0;
  font-size: 28px;
}

.text-info {
  color: #2563eb;
}

.text-success {
  color: #059669;
}

.text-warning {
  color: #d97706;
}

.toolbar {
  padding: 20px;
}

.toolbar-row {
  flex-wrap: wrap;
}

.toolbar-search,
.toolbar-filter {
  display: flex;
  align-items: center;
  gap: 10px;
  border: 1px solid var(--fleet-border);
  border-radius: 14px;
  background: #fff;
  min-height: 46px;
}

.toolbar-search {
  flex: 1 1 360px;
  padding: 0 14px;
}

.toolbar-search input,
.toolbar-filter select,
.field input,
.field select,
.field textarea {
  width: 100%;
  border: none;
  background: transparent;
  color: var(--fleet-text);
  font: inherit;
}

.toolbar-search input:focus,
.toolbar-filter select:focus,
.field input:focus,
.field select:focus,
.field textarea:focus {
  outline: none;
}

.toolbar-actions {
  gap: 12px;
  flex-wrap: wrap;
}

.toolbar-filter {
  min-width: 170px;
  padding: 0 14px;
}

.toolbar-count {
  margin-top: 14px;
  font-size: 14px;
}

.clear-button,
.icon-button,
.primary-button,
.ghost-button {
  border: none;
  cursor: pointer;
}

.clear-button,
.icon-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: transparent;
}

.primary-button,
.ghost-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  min-height: 44px;
  padding: 0 18px;
  border-radius: 12px;
  font-weight: 700;
}

.primary-button {
  background: var(--fleet-primary);
  color: #fff;
}

.primary-button:hover {
  background: var(--fleet-primary-dark);
}

.ghost-button {
  background: #eff6ff;
  color: var(--fleet-primary);
}

.table-card,
.details-card,
.form-card {
  overflow: hidden;
}

.table-wrap {
  overflow-x: auto;
}

.table-base {
  width: 100%;
}

.table-base :deep(table) {
  border-collapse: separate;
  border-spacing: 0;
}

.table-base :deep(thead th) {
  background: #f8fafc;
  color: #475569;
  font-size: 13px;
  letter-spacing: 0.02em;
  text-transform: uppercase;
  font-weight: 700;
  padding: 14px 22px;
}

.table-base :deep(tbody)::before,
.table-base :deep(tbody)::after {
  display: none;
}

.table-base :deep(tbody td) {
  padding: 16px 22px;
  background: #fff;
}

.table-base :deep(tbody tr) {
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.06);
}

.table-base :deep(tbody tr td) {
  border-bottom: 10px solid transparent;
}

.table-base :deep(tbody tr:last-child td) {
  border-bottom: 0;
}

.table-base :deep(tbody tr:nth-child(even) td) {
  background: #f8fafc;
}

.table-base :deep(tbody tr td:first-child) {
  border-radius: 12px 0 0 12px;
}

.table-base :deep(tbody tr td:last-child) {
  border-radius: 0 12px 12px 0;
}

.table-base :deep(thead th:first-child) {
  border-radius: 12px 0 0 12px;
}

.table-base :deep(thead th:last-child) {
  border-radius: 0 12px 12px 0;
}

.table-base :deep(thead th:nth-child(1)),
.table-base :deep(tbody td:nth-child(1)) {
  width: 64px;
}

.table-base :deep(thead th:nth-child(2)),
.table-base :deep(tbody td:nth-child(2)) {
  width: 170px;
}

.table-base :deep(thead th:nth-child(3)),
.table-base :deep(tbody td:nth-child(3)) {
  width: 260px;
}

.table-base :deep(thead th:nth-child(4)),
.table-base :deep(tbody td:nth-child(4)) {
  width: 280px;
}

.table-base :deep(thead th:nth-child(5)),
.table-base :deep(tbody td:nth-child(5)) {
  width: 210px;
}

.table-base :deep(thead th:nth-child(6)),
.table-base :deep(tbody td:nth-child(6)) {
  width: 210px;
}

.table-base :deep(thead th:nth-child(7)),
.table-base :deep(tbody td:nth-child(7)) {
  width: 220px;
}

.table-base :deep(thead th:nth-child(8)),
.table-base :deep(tbody td:nth-child(8)) {
  width: 170px;
}

.table-base :deep(thead th:nth-child(9)),
.table-base :deep(tbody td:nth-child(9)) {
  width: 150px;
}

.table-base :deep(thead th.align-right),
.table-base :deep(tbody td.align-right) {
  text-align: right;
}

.table-base :deep(.v-data-table__th),
.table-base :deep(.v-data-table__td) {
  border-bottom: none;
}

.table-base :deep(.v-data-table-footer) {
  padding: 12px 16px 16px;
  border-top: 1px solid var(--fleet-border);
}

.trip-id-cell,
.stack-cell,
.route-cell {
  display: grid;
  gap: 8px;
  min-width: 0;
}

.row-id {
  font-weight: 700;
  color: #94a3b8;
  font-size: 12px;
  min-width: 28px;
  text-align: right;
}

.trip-sub {
  font-size: 12px;
}

.trip-id-cell strong,
.stack-cell strong,
.route-line span,
.route-arrow,
.stack-cell span {
  overflow-wrap: normal;
  word-break: normal;
  hyphens: none;
}

.route-line {
  display: flex;
  align-items: center;
  gap: 10px;
}

.trip-id-cell strong,
.stack-cell strong {
  line-height: 1.35;
}

.trip-number,
.schedule-primary,
.vehicle-plate,
.badge {
  white-space: nowrap;
}

.stack-cell span,
.route-line span,
.route-arrow {
  line-height: 1.45;
}

.route-arrow {
  font-size: 12px;
}

.route-line .v-icon {
  flex: 0 0 auto;
}

.badge {
  white-space: nowrap;
}

.icon-button {
  width: 34px;
  height: 34px;
  border-radius: 10px;
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

.empty-state {
  padding: 32px;
  color: var(--fleet-muted);
  text-align: center;
}

.tooltip {
  position: relative;
}

.tooltip-text {
  position: absolute;
  bottom: calc(100% + 8px);
  left: 50%;
  transform: translateX(-50%);
  background: #0f172a;
  color: #fff;
  padding: 6px 8px;
  border-radius: 8px;
  font-size: 12px;
  white-space: nowrap;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.18s ease;
}

.tooltip:hover .tooltip-text {
  opacity: 1;
}

.details-card,
.form-card {
  padding: 24px;
}

.form-card {
  display: flex;
  flex-direction: column;
  max-height: min(88vh, 980px);
  padding: 0;
  background:
    linear-gradient(180deg, rgba(248, 250, 252, 0.96) 0%, rgba(255, 255, 255, 1) 180px);
}

.details-title,
.form-title {
  font-size: 22px;
  font-weight: 700;
}

.details-subtitle {
  margin-top: 4px;
}

.details-grid,
.form-grid {
  display: grid;
  gap: 16px;
}

.details-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
  margin-top: 24px;
}

.details-section,
.form-section {
  border: 1px solid var(--fleet-border);
  border-radius: 18px;
  padding: 18px;
  background: #fff;
}

.details-section h4 {
  margin: 0 0 14px;
  font-size: 16px;
}

.details-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding: 10px 0;
  border-bottom: 1px solid #eef2f7;
}

.details-row:last-child {
  border-bottom: none;
  padding-bottom: 0;
}

.details-row span {
  color: var(--fleet-muted);
}

.full-width,
.field.full {
  grid-column: 1 / -1;
}

.notes-grid {
  gap: 14px;
  flex-wrap: wrap;
}

.note-card {
  flex: 1 1 240px;
  padding: 14px;
  border-radius: 14px;
  background: #f8fafc;
}

.note-card p {
  margin: 8px 0 0;
}

.note-label,
.section-heading {
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.section-heading {
  margin-bottom: 16px;
}

.form-layout {
  display: flex;
  flex: 1;
  min-height: 0;
  flex-direction: column;
}

.form-header {
  flex: 0 0 auto;
  padding: 28px 32px 22px;
  border-bottom: 1px solid rgba(226, 232, 240, 0.9);
  background: rgba(255, 255, 255, 0.92);
  backdrop-filter: blur(8px);
}

.form-scroll {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 22px 32px 0;
  display: grid;
  gap: 16px;
}

.form-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.notes-form-grid {
  grid-template-columns: 1fr;
}

.field {
  display: grid;
  gap: 10px;
  min-width: 0;
}

.field label {
  font-size: 13px;
  font-weight: 700;
  color: #0f172a;
}

.field input,
.field select,
.field textarea {
  min-height: 50px;
  border: 1px solid #dbe3ef;
  border-radius: 14px;
  padding: 13px 16px;
  background: #ffffff;
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.8);
}

.field textarea {
  min-height: 120px;
  resize: vertical;
}

.field input::placeholder,
.field textarea::placeholder {
  color: #94a3b8;
}

.field input:hover,
.field select:hover,
.field textarea:hover {
  border-color: #cbd5e1;
}

.field input:focus,
.field select:focus,
.field textarea:focus {
  border-color: rgba(37, 99, 235, 0.45);
  box-shadow: 0 0 0 4px rgba(37, 99, 235, 0.12);
}

.required::after {
  content: " *";
  color: var(--fleet-danger);
}

.checkbox-field {
  align-content: end;
}

.checkbox-field label {
  display: flex;
  align-items: center;
  gap: 10px;
  min-height: 46px;
}

.checkbox-field input[type="checkbox"] {
  width: 18px;
  height: 18px;
  min-height: 18px;
  margin: 0;
}

.form-error {
  margin: 18px 32px 0;
  padding: 13px 16px;
  border-radius: 14px;
  background: #fee2e2;
  color: #b91c1c;
  font-size: 14px;
}

.form-actions {
  flex: 0 0 auto;
  padding: 18px 32px 24px;
  border-top: 1px solid rgba(226, 232, 240, 0.9);
  background: rgba(255, 255, 255, 0.96);
  backdrop-filter: blur(8px);
}

.form-section {
  border: 1px solid #dbe3ef;
  border-radius: 24px;
  padding: 24px;
  background:
    linear-gradient(180deg, rgba(255, 255, 255, 1) 0%, rgba(248, 250, 252, 0.75) 100%);
  box-shadow: 0 12px 28px rgba(15, 23, 42, 0.04);
}

.section-heading {
  margin-bottom: 18px;
  font-size: 12px;
  letter-spacing: 0.08em;
  color: #64748b;
}

@media (max-width: 960px) {
  .trips-page {
    padding: 16px;
  }

  .details-grid,
  .form-grid {
    grid-template-columns: 1fr;
  }

  .form-header,
  .form-scroll,
  .form-actions,
  .form-error {
    padding-left: 20px;
    padding-right: 20px;
  }
}

@media (max-width: 720px) {
  .page-header,
  .form-actions {
    flex-direction: column;
    align-items: stretch;
  }

  .toolbar-actions {
    width: 100%;
  }

  .toolbar-filter {
    flex: 1 1 100%;
  }

  .details-card,
  .toolbar {
    padding: 16px;
  }

  .form-card {
    max-height: 92vh;
  }

  .form-section {
    padding: 18px;
    border-radius: 18px;
  }
}
</style>
