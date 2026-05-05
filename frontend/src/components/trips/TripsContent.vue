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
      <button v-if="canCreateTrips" class="primary-button" type="button" @click="openAddDialog">
        <v-icon icon="mdi-plus" size="18" />
        Create Trip
      </button>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Trips</p>
        <h3>{{ trips.length }}</h3>
      </div>
      <div class="stat-card">
        <p>In Transit</p>
        <h3 class="text-info">{{ inTransitCount }}</h3>
      </div>
      <div class="stat-card">
        <p>Delayed</p>
        <h3 class="text-warning">{{ delayedCount }}</h3>
      </div>
      <div class="stat-card">
        <p>Completed</p>
        <h3 class="text-success">{{ completedCount }}</h3>
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
      :can-edit="canEditTrips"
      :can-delete="canDeleteTrips"
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
      :cargo-type-options="cargoTypes"
      :vehicle-options="vehicleOptions"
      :driver-options="driverOptions"
      :dispatcher-options="dispatcherOptions"
      :location-options="locationOptions"
      :department-options="departmentOptions"
      @close="closeForm"
      @submit="submitForm"
    />
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from "vue";
import TripDetailsDialog from "./TripDetailsDialog.vue";
import TripFormDialog from "./TripFormDialog.vue";
import TripsTable from "./TripsTable.vue";
import { getDepartmentOptions } from "../../services/departmentsApi";
import { getLocationOptions } from "../../services/locationsApi";
import { getUsers } from "../../services/usersApi";
import { getVehicles } from "../../services/vehiclesApi";
import { createTrip, deleteTrip as deleteTripRecord, getTrips, updateTrip } from "../../services/tripsApi";
import { cargoTypesApi, statusesApi, tripPrioritiesApi, tripTypesApi } from "../../services/tripSetupApi";
import { canCreateModule, canDeleteModule, canEditModule } from "../../utils/authSession";

const tripStatuses = ref([]);
const tripTypes = ref([]);
const priorities = ref([]);
const cargoTypes = ref([]);
const vehicleOptions = ref([]);
const driverOptions = ref([]);
const dispatcherOptions = ref([]);
const locationOptions = ref([]);
const departmentOptions = ref([]);

const createEmptyTrip = () => ({
  id: null,
  tripNumber: "",
  tripType: "",
  status: "",
  priority: "",
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

const trips = ref([]);
const canCreateTrips = computed(() => canCreateModule("trips"));
const canEditTrips = computed(() => canEditModule("trips"));
const canDeleteTrips = computed(() => canDeleteModule("trips"));

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
const itemsPerPage = ref(10);
const form = reactive(createEmptyTrip());

const loadTrips = async () => {
  try {
    const result = await getTrips({ page: 1, pageSize: 500 });
    trips.value = result.items || [];
  } catch (error) {
    formError.value = error.message || "Could not load trips.";
  }
};

const loadReferenceOptions = async () => {
  const [types, statuses, priorityOptions, cargoOptions, vehicles, drivers, dispatchers, locations, departments] =
    await Promise.allSettled([
      tripTypesApi.options(),
      statusesApi.options(),
      tripPrioritiesApi.options(),
      cargoTypesApi.options(),
      getVehicles(),
      getUsers({ role: "Driver", status: "Active", pageSize: 500, sortBy: "name" }),
      getUsers({ role: "Dispatcher", status: "Active", pageSize: 500, sortBy: "name" }),
      getLocationOptions(),
      getDepartmentOptions(),
    ]);

  if (types.status === "fulfilled") tripTypes.value = types.value;
  if (statuses.status === "fulfilled") tripStatuses.value = statuses.value;
  if (priorityOptions.status === "fulfilled") priorities.value = priorityOptions.value;
  if (cargoOptions.status === "fulfilled") cargoTypes.value = cargoOptions.value;
  if (vehicles.status === "fulfilled") vehicleOptions.value = vehicles.value;
  if (drivers.status === "fulfilled") driverOptions.value = drivers.value.items || [];
  if (dispatchers.status === "fulfilled") dispatcherOptions.value = dispatchers.value.items || [];
  if (locations.status === "fulfilled") locationOptions.value = locations.value;
  if (departments.status === "fulfilled") departmentOptions.value = departments.value;
};

onMounted(() => {
  loadTrips();
  loadReferenceOptions();
});

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
  itemsPerPage.value = Number(options.itemsPerPage) || 10;
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
  form.tripNumber = `TRP-${String(3100 + trips.value.length + 1)}`;
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

const submitForm = async () => {
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

  try {
    const savedTrip =
      formMode.value === "edit" && editingTripId.value !== null
        ? await updateTrip(editingTripId.value, payload)
        : await createTrip(payload);

    if (formMode.value === "edit" && editingTripId.value !== null) {
      trips.value = trips.value.map((trip) =>
        trip.id === editingTripId.value ? savedTrip : trip,
      );
    } else {
      trips.value = [savedTrip, ...trips.value];
    }

    closeForm();
  } catch (error) {
    formError.value = error.message || "Could not save trip.";
  }
};

const deleteTrip = async (id) => {
  try {
    await deleteTripRecord(id);
    trips.value = trips.value.filter((trip) => trip.id !== id);
    if (selectedTrip.value?.id === id) {
      detailsOpen.value = false;
      selectedTrip.value = null;
    }
  } catch (error) {
    formError.value = error.message || "Could not delete trip.";
  }
};
</script>

<style src="./trips_styles/TripsContent.css"></style>
