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
          <div v-if="showScopeFilter" class="toolbar-filter">
            <v-icon icon="mdi-account-switch-outline" />
            <select v-model="scopeFilter" @change="loadTrips">
              <option value="mine">My Work</option>
              <option value="all">All Work</option>
            </select>
          </div>

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

    <PageMessage
      :tone="pageMessage.tone"
      :title="pageMessage.title"
      :message="pageMessage.message"
      @close="clearPageMessage"
    />

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
      :saving="formSaving"
      @close="closeForm"
      @submit="submitForm"
    />
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from "vue";
import PageMessage from "../common/PageMessage.vue";
import TripDetailsDialog from "./TripDetailsDialog.vue";
import TripFormDialog from "./TripFormDialog.vue";
import TripsTable from "./TripsTable.vue";
import { getDepartmentOptions } from "../../services/departmentsApi";
import { getLocationOptions } from "../../services/locationsApi";
import { getUsers } from "../../services/usersApi";
import { getVehicles } from "../../services/vehiclesApi";
import { createTrip, deleteTrip as deleteTripRecord, getTrips, updateTrip } from "../../services/tripsApi";
import { cargoTypesApi, statusesApi, tripPrioritiesApi, tripTypesApi } from "../../services/tripSetupApi";
import { usePageMessage } from "../../composables/usePageMessage";
import { canCreateModule, canDeleteModule, canEditModule, getCurrentUser } from "../../utils/authSession";
import { validateTripLoadAgainstVehicle } from "../../utils/loadCapacity";

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
const currentUser = computed(() => getCurrentUser());
const currentRole = computed(() => String(currentUser.value?.roleId || currentUser.value?.role || "").toLowerCase());
const showScopeFilter = computed(() => currentRole.value === "driver" || currentRole.value === "dispatcher");

const searchQuery = ref("");
const scopeFilter = ref(showScopeFilter.value ? "mine" : "all");
const statusFilter = ref("All");
const tripTypeFilter = ref("All");
const selectedTrip = ref(null);
const detailsOpen = ref(false);
const formOpen = ref(false);
const formMode = ref("create");
const formError = ref("");
const formSaving = ref(false);
const editingTripId = ref(null);
const tablePage = ref(1);
const itemsPerPage = ref(10);
const form = reactive(createEmptyTrip());
const { pageMessage, clearPageMessage, showPageMessage } = usePageMessage(4000);

const loadTrips = async () => {
  try {
    const result = await getTrips({ page: 1, pageSize: 500, scope: scopeFilter.value });
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
  const selectedVehicle = vehicleOptions.value.find((vehicle) => vehicle.id === form.vehicleId);
  const assignedDriver = String(selectedVehicle?.driver || "").trim();

  const requiredFields = [
    ["Trip number", form.tripNumber],
    ["Trip type", form.tripType],
    ["Status", form.status],
    ["Priority", form.priority],
    ["Customer", form.customerName],
    ["Department", form.department],
    ["Vehicle", form.vehicleId],
    ["Vehicle plate", form.vehiclePlate],
    ["Driver", form.driverName],
    ["Dispatcher", form.dispatcherName],
    ["Cargo type", form.cargoType],
    ["Pickup location", form.pickupLocation],
    ["Dropoff location", form.dropoffLocation],
    ["Departure", form.departureDateTime],
    ["Estimated arrival", form.estimatedArrival],
  ];
  const missingFields = requiredFields
    .filter(([, value]) => String(value ?? "").trim() === "")
    .map(([label]) => label);

  if (missingFields.length) {
    formError.value = `Fill required fields: ${missingFields.join(", ")}.`;
    showPageMessage({
      tone: "error",
      title: "Trip was not saved",
      message: formError.value,
    });
    return;
  }

  if (Number(form.loadWeightKg || 0) <= 0) {
    formError.value = "Load weight must be greater than 0 kg.";
    showPageMessage({
      tone: "error",
      title: "Trip was not saved",
      message: formError.value,
    });
    return;
  }

  if (Number(form.plannedDistanceKm || 0) <= 0) {
    formError.value = "Planned distance must be greater than 0 km.";
    showPageMessage({
      tone: "error",
      title: "Trip was not saved",
      message: formError.value,
    });
    return;
  }

  if (!selectedVehicle) {
    formError.value = "Selected vehicle could not be found.";
    showPageMessage({
      tone: "error",
      title: "Trip was not saved",
      message: formError.value,
    });
    return;
  }

  if (!assignedDriver) {
    formError.value = "Selected vehicle has no assigned driver. Assign a driver to the vehicle before creating a trip.";
    showPageMessage({
      tone: "error",
      title: "Trip was not saved",
      message: formError.value,
    });
    return;
  }

  if (assignedDriver.toLowerCase() !== String(form.driverName || "").trim().toLowerCase()) {
    formError.value = "Selected driver is not assigned to the selected vehicle.";
    showPageMessage({
      tone: "error",
      title: "Trip was not saved",
      message: formError.value,
    });
    return;
  }

  const loadError = validateTripLoadAgainstVehicle(form, selectedVehicle);
  if (loadError) {
    formError.value = loadError;
    showPageMessage({
      tone: "error",
      title: "Trip was not saved",
      message: formError.value,
    });
    return;
  }

  if (
    form.estimatedArrival &&
    form.departureDateTime &&
    form.estimatedArrival < form.departureDateTime
  ) {
    formError.value = "Estimated arrival cannot be earlier than departure.";
    showPageMessage({
      tone: "error",
      title: "Trip was not saved",
      message: formError.value,
    });
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
    formSaving.value = true;
    formError.value = "";
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

    showPageMessage({
      tone: "success",
      title: formMode.value === "edit" ? "Trip updated" : "Trip created",
      message:
        formMode.value === "edit"
          ? `${savedTrip.tripNumber} was updated and the assigned driver will be notified.`
          : `${savedTrip.tripNumber} was created and the assigned driver will be notified.`,
    });
    closeForm();
  } catch (error) {
    formError.value = error.message || "Could not save trip.";
    showPageMessage({
      tone: "error",
      title: "Trip was not saved",
      message: formError.value,
    });
  } finally {
    formSaving.value = false;
  }
};

const deleteTrip = async (id) => {
  try {
    const trip = trips.value.find((item) => item.id === id);
    await deleteTripRecord(id);
    trips.value = trips.value.filter((trip) => trip.id !== id);
    if (selectedTrip.value?.id === id) {
      detailsOpen.value = false;
      selectedTrip.value = null;
    }
    showPageMessage({
      tone: "success",
      title: "Trip deleted",
      message: `${trip?.tripNumber || "Trip"} was deleted successfully.`,
    });
  } catch (error) {
    formError.value = error.message || "Could not delete trip.";
    showPageMessage({
      tone: "error",
      title: "Trip was not deleted",
      message: formError.value,
    });
  }
};
</script>

<style src="./trips_styles/TripsContent.css"></style>
