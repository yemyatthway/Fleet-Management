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

    <TripsTable
      :items="filteredTrips"
      :page="tablePage"
      :items-per-page="itemsPerPage"
      @update:options="handleTableOptions"
      @view="openDetails"
      @edit="openEditDialog"
      @remove="deleteTrip"
    />

    <TripDetailsDialog v-model:open="detailsOpen" :trip="selectedTrip" />

    <TripFormDialog
      v-model:open="formOpen"
      :mode="formMode"
      :form="form"
      :error="formError"
      :trip-statuses="tripStatuses"
      :trip-types="tripTypes"
      :priorities="priorities"
      @close="closeForm"
      @submit="submitForm"
    />
  </div>
</template>

<script setup>
import { computed, reactive, ref } from "vue";
import TripDetailsDialog from "./TripDetailsDialog.vue";
import TripFormDialog from "./TripFormDialog.vue";
import TripsTable from "./TripsTable.vue";

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
        trip.coDriverName,
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

const handleTableOptions = (options) => {
  tablePage.value = Number(options.page) || 1;
  itemsPerPage.value = Number(options.itemsPerPage) || 8;
};

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

<style src="./trips_styles/TripsContent.css"></style>
