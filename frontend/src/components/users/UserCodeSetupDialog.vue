<template>
  <v-dialog v-model="internalOpen" max-width="520">
    <v-card class="dialog-card">
      <div class="dialog-header">
        <h2>{{ mode === 'edit' ? `Edit ${itemLabel}` : `Create ${itemLabel}` }}</h2>
        <button class="icon-button" type="button" @click="close">
          <v-icon icon="mdi-close" />
        </button>
      </div>

      <form class="dialog-body" @submit.prevent="submit">
        <div v-if="!fixedType" class="field">
          <label class="required">Type</label>
          <select v-model="form.type" required>
            <option value="Department">Department</option>
            <option value="Location">Location / Depot</option>
          </select>
        </div>
        <div class="field">
          <label class="required">Name</label>
          <input v-model="form.name" type="text" :placeholder="`Enter ${itemLabel.toLowerCase()} name`" required />
        </div>
        <template v-if="isLocationForm">
          <div class="field">
            <label class="required">Code</label>
            <input v-model="form.code" type="text" placeholder="BG-WH-01" required />
          </div>
          <div class="field">
            <label class="required">Location Type</label>
            <input v-model="form.locationType" type="text" placeholder="Warehouse" required />
          </div>
          <div class="field full">
            <label class="required">Address</label>
            <input v-model="form.address" type="text" placeholder="No. 23, Main Road, Bago" required />
          </div>
          <div class="field">
            <label class="required">City</label>
            <input v-model="form.city" type="text" placeholder="Bago" required />
          </div>
          <div class="field">
            <label class="required">Country</label>
            <input v-model="form.country" type="text" placeholder="Myanmar" required />
          </div>
          <div class="field">
            <label>Contact Person</label>
            <input v-model="form.contactPerson" type="text" placeholder="Ko Aung" />
          </div>
          <div class="field">
            <label class="required">Phone</label>
            <input v-model="form.phone" type="text" placeholder="09-123456789" required />
          </div>
          <div class="field">
            <label class="required">Operating Hours</label>
            <input v-model="form.operatingHours" type="text" placeholder="08:00 - 18:00" required />
          </div>
        </template>
        <div class="field">
          <label>{{ isLocationForm ? 'Notes' : 'Description' }}</label>
          <textarea v-model="form.description" rows="3" :placeholder="isLocationForm ? 'Optional location notes' : 'Optional description'"></textarea>
        </div>
        <div class="field">
          <label>Status</label>
          <select v-model="form.status">
            <option value="Active">Active</option>
            <option value="Disabled">Disabled</option>
          </select>
        </div>

        <p v-if="formError" class="form-error">{{ formError }}</p>

        <div class="dialog-actions">
          <button class="ghost" type="button" @click="close">Cancel</button>
          <button class="primary" type="submit">{{ mode === 'edit' ? 'Save Changes' : `Create ${itemLabel}` }}</button>
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
  fixedType: {
    type: String,
    default: ''
  },
  itemLabel: {
    type: String,
    default: 'Item'
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
  type: 'Department',
  name: '',
  code: '',
  locationType: '',
  address: '',
  city: '',
  country: '',
  contactPerson: '',
  phone: '',
  operatingHours: '',
  description: '',
  status: 'Active'
})

const formError = ref('')
const isLocationForm = computed(() => form.type === 'Location')

const reset = () => {
  form.id = props.item?.id || ''
  form.type = props.item?.type || props.fixedType || 'Department'
  form.name = props.item?.name || ''
  form.code = props.item?.code || ''
  form.locationType = props.item?.locationType || ''
  form.address = props.item?.address || ''
  form.city = props.item?.city || ''
  form.country = props.item?.country || ''
  form.contactPerson = props.item?.contactPerson || ''
  form.phone = props.item?.phone || ''
  form.operatingHours = props.item?.operatingHours || ''
  form.description = props.item?.description || ''
  form.status = props.item?.status || 'Active'
  formError.value = ''
}

watch(
  () => props.open,
  (value) => {
    if (value) reset()
  }
)

watch(
  () => props.fixedType,
  (value) => {
    if (value && props.open && props.mode !== 'edit') {
      form.type = value
    }
  }
)

const close = () => emit('close')

const submit = () => {
  if (!form.type) {
    formError.value = 'Type is required.'
    return
  }
  if (!form.name) {
    formError.value = 'Name is required.'
    return
  }
  if (isLocationForm.value) {
    if (!form.code) {
      formError.value = 'Code is required.'
      return
    }
    if (!form.locationType) {
      formError.value = 'Location type is required.'
      return
    }
    if (!form.address) {
      formError.value = 'Address is required.'
      return
    }
    if (!form.city) {
      formError.value = 'City is required.'
      return
    }
    if (!form.country) {
      formError.value = 'Country is required.'
      return
    }
    if (!form.phone) {
      formError.value = 'Phone is required.'
      return
    }
    if (!form.operatingHours) {
      formError.value = 'Operating hours are required.'
      return
    }
  }

  formError.value = ''
  emit('save', { ...form })
}
</script>

<style scoped src="../roles/roles_styles/RoleDialog.css"></style>
