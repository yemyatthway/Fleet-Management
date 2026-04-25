<template>
  <div class="trips-dialog" v-if="open">
    <div class="dialog-overlay" @click="$emit('close')"></div>
    <div class="dialog-content">
      <h2>{{ mode === "edit" ? "Edit Trip" : "Add Trip" }}</h2>
      <form @submit.prevent="handleSubmit">
        <div class="form-row">
          <label>Driver</label>
          <select v-model="form.driverId" required>
            <option
              v-for="driver in drivers"
              :key="driver.id"
              :value="driver.id"
            >
              {{ driver.name }}
            </option>
          </select>
        </div>
        <div class="form-row">
          <label>Vehicle</label>
          <select v-model="form.vehicleId" required>
            <option
              v-for="vehicle in vehicles"
              :key="vehicle.id"
              :value="vehicle.id"
            >
              {{ vehicle.name }}
            </option>
          </select>
        </div>
        <div class="form-row">
          <label>Department</label>
          <select v-model="form.departmentId">
            <option v-for="dept in departments" :key="dept.id" :value="dept.id">
              {{ dept.name }}
            </option>
          </select>
        </div>
        <div class="form-row">
          <label>Origin</label>
          <input v-model="form.origin" required />
        </div>
        <div class="form-row">
          <label>Destination</label>
          <input v-model="form.destination" required />
        </div>
        <div class="form-row">
          <label>Start Time</label>
          <input type="datetime-local" v-model="form.startTime" required />
        </div>
        <div class="form-row">
          <label>End Time</label>
          <input type="datetime-local" v-model="form.endTime" />
        </div>
        <div class="form-row">
          <label>Status</label>
          <select v-model="form.status">
            <option value="Active">Active</option>
            <option value="Completed">Completed</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>
        <div class="form-row">
          <label>Notes</label>
          <textarea v-model="form.notes"></textarea>
        </div>
        <div class="form-actions">
          <button type="submit" class="primary-button">
            {{ mode === "edit" ? "Save" : "Add" }}
          </button>
          <button type="button" @click="$emit('close')">Cancel</button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { reactive, watch, toRefs } from "vue";
const props = defineProps({
  open: Boolean,
  mode: String,
  trip: Object,
  drivers: Array,
  vehicles: Array,
  locations: Array,
  departments: Array,
});
const emit = defineEmits(["close", "save"]);

const defaultForm = {
  driverId: "",
  vehicleId: "",
  departmentId: "",
  origin: "",
  destination: "",
  startTime: "",
  endTime: "",
  status: "Active",
  notes: "",
};
const form = reactive({ ...defaultForm });

watch(
  () => props.trip,
  (newTrip) => {
    if (props.mode === "edit" && newTrip) {
      Object.assign(form, newTrip);
    } else {
      Object.assign(form, defaultForm);
    }
  },
  { immediate: true },
);

function handleSubmit() {
  emit("save", { ...form });
}
</script>

<style scoped>
.trips-dialog {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 1000;
}
.dialog-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.2);
}
.dialog-content {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  background: #fff;
  padding: 2rem;
  border-radius: 8px;
  min-width: 350px;
  max-width: 90vw;
}
.form-row {
  margin-bottom: 1rem;
  display: flex;
  flex-direction: column;
}
.form-actions {
  display: flex;
  gap: 1rem;
  justify-content: flex-end;
}
</style>
