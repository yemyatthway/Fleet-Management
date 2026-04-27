<template>
  <v-dialog :model-value="open" max-width="1160" @update:model-value="updateOpen">
    <div class="card-surface form-card">
      <div class="form-header">
        <div>
          <div class="form-title">{{ mode === 'edit' ? 'Edit Trip' : 'Create Trip' }}</div>
          <div class="text-muted">Frontend-only first pass for real dispatch operations.</div>
        </div>
        <button class="icon-button" type="button" @click="$emit('close')">
          <v-icon icon="mdi-close" size="18" />
        </button>
      </div>

      <div v-if="error" class="form-error">{{ error }}</div>

      <form class="form-layout" @submit.prevent="$emit('submit')">
        <div class="form-scroll">
          <div class="form-section">
            <div class="section-heading">Trip Basics</div>
            <div class="form-grid">
              <div class="field">
                <label class="required">Trip Number</label>
                <input v-model.trim="form.tripNumber" type="text" placeholder="TRP-3101" required />
              </div>
              <div class="field">
                <label class="required">Trip Type</label>
                <select v-model="form.tripType" required>
                  <option v-for="type in tripTypes" :key="type" :value="type">{{ type }}</option>
                </select>
              </div>
              <div class="field">
                <label class="required">Status</label>
                <select v-model="form.status" required>
                  <option v-for="status in tripStatuses" :key="status" :value="status">{{ status }}</option>
                </select>
              </div>
              <div class="field">
                <label class="required">Priority</label>
                <select v-model="form.priority" required>
                  <option v-for="priority in priorities" :key="priority" :value="priority">{{ priority }}</option>
                </select>
              </div>
              <div class="field">
                <label class="required">Customer</label>
                <input v-model.trim="form.customerName" type="text" placeholder="Metro Retail Group" required />
              </div>
              <div class="field">
                <label class="required">Department</label>
                <input v-model.trim="form.department" type="text" placeholder="Distribution" required />
              </div>
              <div class="field">
                <label>Cost Center</label>
                <input v-model.trim="form.costCenter" type="text" placeholder="OPS-NORTH-01" />
              </div>
              <div class="field">
                <label>Trailer Number</label>
                <input v-model.trim="form.trailerNumber" type="text" placeholder="TRL-102" />
              </div>
            </div>
          </div>

          <div class="form-section">
            <div class="section-heading">Dispatch Assignment</div>
            <div class="form-grid">
              <div class="field">
                <label class="required">Vehicle ID</label>
                <input v-model.trim="form.vehicleId" type="text" placeholder="FL-2218" required />
              </div>
              <div class="field">
                <label class="required">Vehicle Plate</label>
                <input v-model.trim="form.vehiclePlate" type="text" placeholder="YGN-8K/2218" required />
              </div>
              <div class="field">
                <label class="required">Driver Name</label>
                <input v-model.trim="form.driverName" type="text" placeholder="Aung Kyaw Min" required />
              </div>
              <div class="field">
                <label>Co-driver Name</label>
                <input v-model.trim="form.coDriverName" type="text" placeholder="Optional co-driver" />
              </div>
              <div class="field">
                <label class="required">Dispatcher</label>
                <input v-model.trim="form.dispatcherName" type="text" placeholder="Nilar Htun" required />
              </div>
              <div class="field">
                <label class="required">Cargo Type</label>
                <input v-model.trim="form.cargoType" type="text" placeholder="Cold chain produce" required />
              </div>
              <div class="field">
                <label class="required">Load Weight (kg)</label>
                <input v-model.number="form.loadWeightKg" type="number" min="0" placeholder="18000" required />
              </div>
              <div class="field">
                <label>Load Volume (m3)</label>
                <input v-model.number="form.loadVolumeM3" type="number" min="0" step="0.1" placeholder="42.5" />
              </div>
            </div>
          </div>

          <div class="form-section">
            <div class="section-heading">Route, Timing, and Contacts</div>
            <div class="form-grid">
              <div class="field">
                <label class="required">Pickup Location</label>
                <input v-model.trim="form.pickupLocation" type="text" placeholder="Yangon Distribution Hub" required />
              </div>
              <div class="field">
                <label class="required">Dropoff Location</label>
                <input v-model.trim="form.dropoffLocation" type="text" placeholder="Mandalay Regional DC" required />
              </div>
              <div class="field">
                <label>Pickup Contact</label>
                <input v-model.trim="form.pickupContact" type="text" placeholder="Ko Min • +95 9 000 000000" />
              </div>
              <div class="field">
                <label>Dropoff Contact</label>
                <input v-model.trim="form.dropoffContact" type="text" placeholder="Ma Ei • +95 9 111 111111" />
              </div>
              <div class="field">
                <label class="required">Departure</label>
                <input v-model="form.departureDateTime" type="datetime-local" required />
              </div>
              <div class="field">
                <label class="required">Estimated Arrival</label>
                <input v-model="form.estimatedArrival" type="datetime-local" required />
              </div>
              <div class="field">
                <label>Actual Arrival</label>
                <input v-model="form.actualArrival" type="datetime-local" />
              </div>
              <div class="field">
                <label class="required">Planned Distance (km)</label>
                <input v-model.number="form.plannedDistanceKm" type="number" min="0" placeholder="622" required />
              </div>
            </div>
          </div>

          <div class="form-section">
            <div class="section-heading">Fuel, Odometer, and Compliance</div>
            <div class="form-grid">
              <div class="field">
                <label class="required">Start Odometer (km)</label>
                <input v-model.number="form.startingOdometerKm" type="number" min="0" placeholder="129500" required />
              </div>
              <div class="field">
                <label>Current Odometer (km)</label>
                <input v-model.number="form.currentOdometerKm" type="number" min="0" placeholder="130120" />
              </div>
              <div class="field">
                <label>End Odometer (km)</label>
                <input v-model.number="form.endingOdometerKm" type="number" min="0" placeholder="130156" />
              </div>
              <div class="field">
                <label>Fuel Issued (L)</label>
                <input v-model.number="form.fuelIssuedLiters" type="number" min="0" step="0.1" placeholder="195" />
              </div>
              <div class="field">
                <label>Toll Estimate</label>
                <input v-model.number="form.tollEstimate" type="number" min="0" step="0.01" placeholder="85000" />
              </div>
              <div class="field">
                <label>Temperature Range</label>
                <input v-model.trim="form.temperatureRange" type="text" placeholder="2C to 8C" />
              </div>
              <div class="field checkbox-field">
                <label><input v-model="form.permitRequired" type="checkbox" /> Permit required</label>
              </div>
              <div class="field checkbox-field">
                <label><input v-model="form.temperatureControlled" type="checkbox" /> Temperature controlled</label>
              </div>
            </div>
          </div>

          <div class="form-section">
            <div class="section-heading">Instructions and Notes</div>
            <div class="form-grid notes-form-grid">
              <div class="field full">
                <label>Special Instructions</label>
                <textarea v-model.trim="form.specialInstructions" rows="3" placeholder="Dock appointment, gate pass, unloading conditions..." />
              </div>
              <div class="field full">
                <label>Driver Notes</label>
                <textarea v-model.trim="form.driverNotes" rows="3" placeholder="Traffic, incident, waiting time, or field notes..." />
              </div>
            </div>
          </div>
        </div>

        <div class="form-actions">
          <button class="ghost-button" type="button" @click="$emit('close')">Cancel</button>
          <button class="primary-button" type="submit">
            {{ mode === 'edit' ? 'Save Changes' : 'Create Trip' }}
          </button>
        </div>
      </form>
    </div>
  </v-dialog>
</template>

<script setup>
defineProps({
  open: {
    type: Boolean,
    default: false
  },
  mode: {
    type: String,
    default: 'create'
  },
  form: {
    type: Object,
    required: true
  },
  error: {
    type: String,
    default: ''
  },
  tripStatuses: {
    type: Array,
    required: true
  },
  tripTypes: {
    type: Array,
    required: true
  },
  priorities: {
    type: Array,
    required: true
  }
})

const emit = defineEmits(['update:open', 'close', 'submit'])

const updateOpen = (value) => {
  if (!value) emit('close')
  emit('update:open', value)
}
</script>
