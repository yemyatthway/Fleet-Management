<template>
  <v-dialog v-model="internalOpen" max-width="640">
    <v-card class="dialog-card">
      <div class="dialog-header">
        <h2>{{ mode === "create" ? "Create Ticket" : "Edit Ticket" }}</h2>
        <button class="icon-button" type="button" @click="close">
          <v-icon icon="mdi-close" />
        </button>
      </div>

      <form class="dialog-body ticket-form" @submit.prevent="submit">
        <div class="field-grid">
          <div class="field">
            <label class="required">Vehicle</label>
            <input
              v-model.trim="form.vehicle"
              type="text"
              placeholder="Box Truck"
              readonly
              required
            />
          </div>

          <div class="field">
            <label class="required">Vehicle ID</label>
            <select
              v-model="form.vehicleId"
              required
              @change="syncSelectedVehicle"
            >
              <option value="" disabled>Select vehicle</option>
              <option
                v-for="vehicle in vehicleOptions"
                :key="vehicle.id"
                :value="vehicle.id"
              >
                {{ vehicle.id }} •
                {{ vehicle.plate || vehicle.model || vehicle.type }}
              </option>
            </select>
          </div>

          <div class="field">
            <label class="required">Issue</label>
            <select
              v-model="form.issue"
              required
            >
              <option value="" disabled>Select issue</option>
              <option
                v-for="issue in issueOptions"
                :key="issue"
                :value="issue"
              >
                {{ issue }}
              </option>
            </select>
          </div>

          <div class="field">
            <label class="required">Mechanic</label>
            <select v-model="form.mechanic" required>
              <option value="" disabled>Select mechanic</option>
              <option
                v-for="mechanic in mechanicOptions"
                :key="mechanic"
                :value="mechanic"
              >
                {{ mechanic }}
              </option>
            </select>
          </div>

          <div class="field">
            <label class="required">Reported Date</label>
            <input v-model="form.reportedDate" type="date" required />
          </div>

          <div class="field">
            <label class="required">Status</label>
            <select v-model="form.status" required>
              <option value="" disabled>Select status</option>
              <option
                v-for="status in statusOptions"
                :key="status"
                :value="status"
              >
                {{ status }}
              </option>
            </select>
          </div>
        </div>

        <div class="field">
          <label class="required">Details</label>
          <textarea
            v-model.trim="form.details"
            rows="3"
            placeholder="Short description of the maintenance issue"
            required
          />
        </div>

        <p v-if="formError" class="form-error">{{ formError }}</p>

        <div class="dialog-actions">
          <button class="ghost" type="button" @click="close">Cancel</button>
          <button class="primary" type="submit">
            {{ mode === "create" ? "Create Ticket" : "Save Changes" }}
          </button>
        </div>
      </form>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { computed, reactive, ref, watch } from "vue";

const props = defineProps({
  open: {
    type: Boolean,
    default: false,
  },
  mode: {
    type: String,
    default: "create",
  },
  ticket: {
    type: Object,
    default: null,
  },
  mechanics: {
    type: Array,
    default: () => [],
  },
  vehicles: {
    type: Array,
    default: () => [],
  },
  issues: {
    type: Array,
    default: () => [],
  },
  statuses: {
    type: Array,
    default: () => [],
  },
});

const emit = defineEmits(["close", "save"]);

const internalOpen = computed({
  get: () => props.open,
  set: (value) => {
    if (!value) emit("close");
  },
});

const form = reactive({
  id: "",
  vehicle: "",
  vehicleId: "",
  issue: "",
  details: "",
  reportedDate: "",
  mechanic: "",
  status: "",
});

const formError = ref("");

const mechanicOptions = computed(() => {
  const options = props.mechanics.filter(Boolean);
  if (form.mechanic && !options.includes(form.mechanic)) {
    return [form.mechanic, ...options];
  }
  return options;
});

const vehicleOptions = computed(() => {
  const options = props.vehicles.filter((vehicle) => vehicle?.id);
  const hasSelectedVehicle = options.some(
    (vehicle) => vehicle.id === form.vehicleId,
  );
  if (form.vehicleId && !hasSelectedVehicle) {
    return [
      {
        id: form.vehicleId,
        type: form.vehicle,
        model: form.vehicle,
        plate: form.vehicle,
      },
      ...options,
    ];
  }
  return options;
});

const issueOptions = computed(() => {
  const options = props.issues.filter(Boolean);
  if (form.issue && !options.includes(form.issue)) {
    return [form.issue, ...options];
  }
  return options;
});

const statusOptions = computed(() => {
  const options = props.statuses.filter(Boolean);
  if (form.status && !options.includes(form.status)) {
    return [form.status, ...options];
  }
  return options;
});

const reset = () => {
  form.id = props.ticket?.id || "";
  form.vehicle = props.ticket?.vehicle || "";
  form.vehicleId = props.ticket?.vehicleId || "";
  form.issue = props.ticket?.issue || "";
  form.details = props.ticket?.details || "";
  form.reportedDate = props.ticket?.reportedDate || "";
  form.mechanic = props.ticket?.mechanic || "";
  form.status = props.ticket?.status || props.statuses[0] || "";
  formError.value = "";
};

watch(
  () => props.open,
  (value) => {
    if (value) reset();
  },
);

const close = () => emit("close");

const getVehicleLabel = (vehicle) =>
  vehicle?.type || vehicle?.model || vehicle?.plate || vehicle?.id || "";

const syncSelectedVehicle = () => {
  const selectedVehicle = vehicleOptions.value.find(
    (vehicle) => vehicle.id === form.vehicleId,
  );
  if (!selectedVehicle) return;
  form.vehicle = getVehicleLabel(selectedVehicle);
};

const submit = () => {
  if (
    !form.vehicle ||
    !form.vehicleId ||
    !form.issue ||
    !form.details ||
    !form.reportedDate ||
    !form.mechanic ||
    !form.status
  ) {
    formError.value = "Please complete all required fields.";
    return;
  }

  formError.value = "";
  emit("save", { ...form });
};
</script>

<style scoped src="../roles/roles_styles/RoleDialog.css"></style>

<style scoped src="./maintenance_styles/MaintenanceTicketDialog.css"></style>
