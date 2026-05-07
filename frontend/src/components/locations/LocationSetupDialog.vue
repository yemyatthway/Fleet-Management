<template>
  <v-dialog v-model="internalOpen" max-width="760">
    <v-card class="dialog-card">
      <div class="dialog-header">
        <h2>{{ mode === 'edit' ? 'Edit Location' : 'Create Location' }}</h2>
        <button class="icon-button" type="button" @click="close">
          <v-icon icon="mdi-close" />
        </button>
      </div>

      <form class="dialog-body location-form" @submit.prevent="submit">
        <div class="field-grid">
          <div class="field">
            <label class="required">Name</label>
            <input v-model.trim="form.name" type="text" placeholder="Bago Main Warehouse" required />
          </div>

          <div class="field">
            <label class="required">Code</label>
            <input v-model.trim="form.code" type="text" placeholder="BG-WH-01" required />
          </div>

          <div class="field">
            <label class="required">Type</label>
            <select v-model="form.type">
              <option v-for="type in locationTypes" :key="type" :value="type">
                {{ type }}
              </option>
            </select>
          </div>

          <div class="field">
            <label class="required">Status</label>
            <select v-model="form.status">
              <option value="Active">Active</option>
              <option value="Disabled">Disabled</option>
            </select>
          </div>
        </div>

        <div class="field">
          <label class="required">Address</label>
          <input v-model.trim="form.address" type="text" placeholder="No. 23, Main Road, Bago" required />
        </div>

        <div class="field-grid">
          <div class="field">
            <label class="required">City</label>
            <input v-model.trim="form.city" type="text" placeholder="Bago" required />
          </div>

          <div class="field">
            <label class="required">Country</label>
            <input v-model.trim="form.country" type="text" placeholder="Myanmar" required />
          </div>

          <div class="field">
            <label>Contact Person</label>
            <input v-model.trim="form.contactPerson" type="text" placeholder="Ko Aung" />
          </div>

          <div class="field">
            <label class="required">Phone</label>
            <input v-model.trim="form.phone" type="text" placeholder="09-123456789" required />
          </div>
        </div>

        <div class="field">
          <label class="required">Operating Hours</label>
          <input v-model.trim="form.operatingHours" type="text" placeholder="08:00 - 18:00" required />
        </div>

        <div class="field">
          <label>Notes</label>
          <textarea
            v-model.trim="form.notes"
            rows="3"
            placeholder="Near highway, easy truck access"
          />
        </div>

        <p v-if="formError" class="form-error">{{ formError }}</p>

        <div class="dialog-actions">
          <button class="ghost" type="button" @click="close">Cancel</button>
          <button class="primary" type="submit">{{ mode === 'edit' ? 'Save Changes' : 'Create Location' }}</button>
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
    default: 'add'
  },
  item: {
    type: Object,
    default: null
  },
  locationTypes: {
    type: Array,
    default: () => ['Warehouse', 'Depot', 'Hub', 'Yard', 'Office']
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
  name: '',
  code: '',
  type: 'Warehouse',
  address: '',
  city: '',
  country: 'Myanmar',
  contactPerson: '',
  phone: '',
  operatingHours: '08:00 - 18:00',
  status: 'Active',
  notes: ''
})

const formError = ref('')

const reset = () => {
  form.id = props.item?.id || ''
  form.name = props.item?.name || ''
  form.code = props.item?.code || ''
  form.type = props.item?.type || 'Warehouse'
  form.address = props.item?.address || ''
  form.city = props.item?.city || ''
  form.country = props.item?.country || 'Myanmar'
  form.contactPerson = props.item?.contactPerson || ''
  form.phone = props.item?.phone || ''
  form.operatingHours = props.item?.operatingHours || '08:00 - 18:00'
  form.status = props.item?.status || 'Active'
  form.notes = props.item?.notes || ''
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
  if (
    !form.name ||
    !form.code ||
    !form.type ||
    !form.address ||
    !form.city ||
    !form.country ||
    !form.phone ||
    !form.operatingHours
  ) {
    formError.value = 'Please complete all required fields.'
    return
  }

  formError.value = ''
  emit('save', { ...form })
}
</script>

<style scoped src="../roles/roles_styles/RoleDialog.css"></style>

<style scoped src="./locations_styles/LocationSetupDialog.css"></style>
