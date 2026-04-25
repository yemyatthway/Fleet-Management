<template>
  <v-dialog v-model="internalOpen" max-width="640">
    <v-card class="dialog-card">
      <div class="dialog-header">
        <h2>{{ mode === 'create' ? 'Create Ticket' : 'Edit Ticket' }}</h2>
        <button class="icon-button" type="button" @click="close">
          <v-icon icon="mdi-close" />
        </button>
      </div>

      <form class="dialog-body ticket-form" @submit.prevent="submit">
        <div class="field-grid">
          <div class="field">
            <label class="required">Vehicle</label>
            <input v-model.trim="form.vehicle" type="text" placeholder="Box Truck" required />
          </div>

          <div class="field">
            <label class="required">Vehicle ID</label>
            <input v-model.trim="form.vehicleId" type="text" placeholder="VH-2048" required />
          </div>

          <div class="field">
            <label class="required">Issue</label>
            <input v-model.trim="form.issue" type="text" placeholder="Brake Inspection" required />
          </div>

          <div class="field">
            <label class="required">Mechanic</label>
            <select v-model="form.mechanic" required>
              <option value="" disabled>Select mechanic</option>
              <option v-for="mechanic in mechanicOptions" :key="mechanic" :value="mechanic">
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
              <option value="Pending">Pending</option>
              <option value="Repairing">Repairing</option>
              <option value="Completed">Completed</option>
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
            {{ mode === 'create' ? 'Create Ticket' : 'Save Changes' }}
          </button>
        </div>
      </form>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { computed, reactive, ref, watch } from 'vue'

const props = defineProps({
  open: {
    type: Boolean,
    default: false
  },
  mode: {
    type: String,
    default: 'create'
  },
  ticket: {
    type: Object,
    default: null
  },
  mechanics: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['close', 'save'])

const internalOpen = computed({
  get: () => props.open,
  set: (value) => {
    if (!value) emit('close')
  }
})

const form = reactive({
  id: '',
  vehicle: '',
  vehicleId: '',
  issue: '',
  details: '',
  reportedDate: '',
  mechanic: '',
  status: 'Pending'
})

const formError = ref('')

const mechanicOptions = computed(() => {
  const options = props.mechanics.filter(Boolean)
  if (form.mechanic && !options.includes(form.mechanic)) {
    return [form.mechanic, ...options]
  }
  return options
})

const reset = () => {
  form.id = props.ticket?.id || ''
  form.vehicle = props.ticket?.vehicle || ''
  form.vehicleId = props.ticket?.vehicleId || ''
  form.issue = props.ticket?.issue || ''
  form.details = props.ticket?.details || ''
  form.reportedDate = props.ticket?.reportedDate || ''
  form.mechanic = props.ticket?.mechanic || ''
  form.status = props.ticket?.status || 'Pending'
  formError.value = ''
}

watch(
  () => props.open,
  (value) => {
    if (value) reset()
  }
)

const close = () => emit('close')

const submit = () => {
  if (!form.vehicle || !form.vehicleId || !form.issue || !form.details || !form.reportedDate || !form.mechanic) {
    formError.value = 'Please complete all required fields.'
    return
  }

  formError.value = ''
  emit('save', { ...form })
}
</script>

<style scoped src="../roles/roles_styles/RoleDialog.css"></style>

<style scoped>
.ticket-form {
  gap: 18px;
}

.field-grid {
  display: grid;
  gap: 16px;
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

@media (max-width: 640px) {
  .field-grid {
    grid-template-columns: 1fr;
  }
}
</style>
