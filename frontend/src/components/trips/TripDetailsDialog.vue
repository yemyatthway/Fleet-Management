<template>
  <v-dialog
    :model-value="open"
    max-width="1100"
    scrollable
    @update:model-value="updateOpen"
  >
    <div v-if="trip" class="card-surface details-card">
      <div class="details-header">
        <div>
          <div class="details-title">
            {{ trip.tripNumber }} • {{ trip.customerName }}
          </div>
          <div class="details-subtitle text-muted">
            {{ trip.tripType }} • {{ trip.status }} • {{ trip.priority }}
          </div>
        </div>
        <button class="icon-button" type="button" @click="updateOpen(false)">
          <v-icon icon="mdi-close" size="18" />
        </button>
      </div>

      <div class="details-scroll">
        <div class="details-grid">
          <div class="details-section">
            <h4>Dispatch</h4>
            <div class="details-row">
              <span>Vehicle</span
              ><strong>{{ trip.vehiclePlate }} ({{ trip.vehicleId }})</strong>
            </div>
            <div class="details-row">
              <span>Driver</span><strong>{{ trip.driverName }}</strong>
            </div>
            <div class="details-row">
              <span>Co-driver</span
              ><strong>{{ trip.coDriverName || "—" }}</strong>
            </div>
            <div class="details-row">
              <span>Dispatcher</span><strong>{{ trip.dispatcherName }}</strong>
            </div>
            <div class="details-row">
              <span>Department</span><strong>{{ trip.department }}</strong>
            </div>
            <div class="details-row">
              <span>Cost Center</span
              ><strong>{{ trip.costCenter || "—" }}</strong>
            </div>
          </div>

          <div class="details-section">
            <h4>Route & Schedule</h4>
            <div class="details-row">
              <span>Pickup</span><strong>{{ trip.pickupLocation }}</strong>
            </div>
            <div class="details-row">
              <span>Dropoff</span><strong>{{ trip.dropoffLocation }}</strong>
            </div>
            <div class="details-row">
              <span>Departure</span
              ><strong>{{ formatDateTime(trip.departureDateTime) }}</strong>
            </div>
            <div class="details-row">
              <span>ETA</span
              ><strong>{{ formatDateTime(trip.estimatedArrival) }}</strong>
            </div>
            <div class="details-row">
              <span>Arrival</span
              ><strong>{{ formatDateTime(trip.actualArrival) }}</strong>
            </div>
            <div class="details-row">
              <span>Distance</span
              ><strong>{{ trip.plannedDistanceKm }} km</strong>
            </div>
          </div>

          <div class="details-section">
            <h4>Load & Service</h4>
            <div class="details-row">
              <span>Customer</span><strong>{{ trip.customerName }}</strong>
            </div>
            <div class="details-row">
              <span>Cargo Type</span><strong>{{ trip.cargoType }}</strong>
            </div>
            <div class="details-row">
              <span>Weight</span
              ><strong>{{ trip.loadWeightKg.toLocaleString() }} kg</strong>
            </div>
            <div class="details-row">
              <span>Volume</span><strong>{{ trip.loadVolumeM3 }} m3</strong>
            </div>
            <div class="details-row">
              <span>Pickup Contact</span
              ><strong>{{ trip.pickupContact || "—" }}</strong>
            </div>
            <div class="details-row">
              <span>Dropoff Contact</span
              ><strong>{{ trip.dropoffContact || "—" }}</strong>
            </div>
          </div>

          <div class="details-section">
            <h4>Fuel & Odometer</h4>
            <div class="details-row">
              <span>Start Odometer</span
              ><strong
                >{{ trip.startingOdometerKm.toLocaleString() }} km</strong
              >
            </div>
            <div class="details-row">
              <span>Current Odometer</span
              ><strong>{{ trip.currentOdometerKm.toLocaleString() }} km</strong>
            </div>
            <div class="details-row">
              <span>End Odometer</span
              ><strong>{{ formatNumber(trip.endingOdometerKm, " km") }}</strong>
            </div>
            <div class="details-row">
              <span>Fuel Issued</span
              ><strong>{{ trip.fuelIssuedLiters }} L</strong>
            </div>
            <div class="details-row">
              <span>Toll Estimate</span
              ><strong>{{ formatCurrency(trip.tollEstimate) }}</strong>
            </div>
            <div class="details-row">
              <span>Temperature Controlled</span
              ><strong>{{ trip.temperatureControlled ? "Yes" : "No" }}</strong>
            </div>
          </div>

          <div class="details-section full-width">
            <h4>Operational Notes</h4>
            <div class="notes-grid">
              <div class="note-card">
                <span class="note-label">Special Instructions</span>
                <p>{{ trip.specialInstructions || "—" }}</p>
              </div>
              <div class="note-card">
                <span class="note-label">Driver Notes</span>
                <p>{{ trip.driverNotes || "—" }}</p>
              </div>
              <div class="note-card">
                <span class="note-label">Compliance</span>
                <p>
                  {{
                    trip.permitRequired
                      ? "Permit required"
                      : "No permit required"
                  }}
                </p>
                <p v-if="trip.temperatureControlled" class="text-muted">
                  Temperature range: {{ trip.temperatureRange || "Not set" }}
                </p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </v-dialog>
</template>

<script setup>
defineProps({
  open: {
    type: Boolean,
    default: false,
  },
  trip: {
    type: Object,
    default: null,
  },
});

const emit = defineEmits(["update:open"]);

const updateOpen = (value) => emit("update:open", value);

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
</script>
