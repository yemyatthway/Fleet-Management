<template>
  <v-dialog v-model="internalOpen" max-width="480">
    <v-card class="dialog-card">
      <div class="dialog-header">
        <h2>Add New User</h2>
        <button class="icon-button" type="button" @click="close">
          <v-icon icon="mdi-close" />
        </button>
      </div>

      <form class="dialog-body" @submit.prevent="submit">
        <div class="form-steps">
          <button
            v-for="step in formSteps"
            :key="step.id"
            type="button"
            class="step-button"
            :class="{ active: formStep === step.id }"
            @click="goToStep(step.id)"
          >
            <span class="step-id">{{ step.id }}</span>
            <span>{{ step.title }}</span>
          </button>
        </div>

        <div v-if="formStep === 1" class="form-section form-grid">
          <div class="field">
            <label class="required">Full Name</label>
            <input v-model="form.name" type="text" placeholder="John Doe" required />
          </div>
          <div class="field">
            <label class="required">Employee ID</label>
            <input v-model="form.employeeId" type="text" placeholder="EMP-1021" required />
          </div>
          <div class="field full">
            <label class="required">NRC</label>
            <input
              v-model.trim="form.nrcNumber"
              type="text"
              placeholder="9/ZaYaTha/111111"
              pattern="\d{1,2}/[A-Za-z]+/\d{6}"
              required
            />
            <div class="field-hint text-muted">Example Format: 9/ZaYaTha/111111</div>
          </div>
          <div class="field">
            <label class="required">Email Address</label>
            <input v-model="form.email" type="email" placeholder="john.doe@fleet.com" required />
          </div>
          <div class="field">
            <label class="required">Phone Number</label>
            <input v-model.trim="form.phone" type="tel" placeholder="+1 (555) 123-4567" required />
          </div>
          <div class="field">
            <label class="required">Role</label>
            <select v-model="form.role" required>
              <option v-for="role in roleOptions" :key="role" :value="role">
                {{ role }}
              </option>
            </select>
          </div>
          <div class="field">
            <label>Status</label>
            <select v-model="form.status">
              <option value="Active">Active</option>
              <option value="Disabled">Disabled</option>
            </select>
          </div>
        </div>

        <div v-if="formStep === 2" class="form-section form-grid">
          <div class="field">
            <label class="required">Job Title</label>
            <input v-model="form.title" type="text" placeholder="Dispatcher" required />
          </div>
          <div class="field">
            <label class="required">Department</label>
            <select v-model="form.department" required>
              <option value="" disabled>Select department</option>
              <option v-for="department in departmentChoices" :key="department" :value="department">
                {{ department }}
              </option>
            </select>
          </div>
          <div class="field">
            <label class="required">Location / Depot</label>
            <input v-model="form.location" type="text" placeholder="Bago Main Warehouse" required />
          </div>
          <div class="field">
            <label class="required">Manager</label>
            <input v-model="form.manager" type="text" placeholder="Sarah Johnson" required />
          </div>
          <div v-if="form.role === 'Driver'" class="field">
            <label class="required">License Number</label>
            <input v-model="form.licenseNumber" type="text" placeholder="D1234567" required />
          </div>
          <div v-if="form.role === 'Driver'" class="field">
            <label class="required">License Class</label>
            <input v-model="form.licenseClass" type="text" placeholder="A" required />
          </div>
          <div v-if="form.role === 'Driver'" class="field">
            <label class="required">License Expiry</label>
            <input v-model="form.licenseExpiry" type="date" required />
          </div>
        </div>

        <div v-if="formStep === 3" class="form-section form-grid">
          <div class="field">
            <label class="required">Emergency Contact Name</label>
            <input v-model="form.emergencyContactName" type="text" placeholder="Jane Doe" required />
          </div>
          <div class="field">
            <label class="required">Emergency Contact Relation</label>
            <input v-model="form.emergencyContactRelation" type="text" placeholder="Spouse" required />
          </div>
          <div class="field">
            <label class="required">Emergency Contact Phone</label>
            <input v-model.trim="form.emergencyContactPhone" type="tel" placeholder="+1 (555) 222-3344" required />
          </div>
          <div class="field">
            <label class="required">Address</label>
            <input v-model="form.address" type="text" placeholder="120 Market St, Springfield, IL" required />
          </div>
          <div class="field checkbox-field">
            <label>
              <input v-model="form.twoFactorEnabled" type="checkbox" />
              Two-factor enabled
            </label>
          </div>
          <div class="field">
            <label>Notes</label>
            <textarea v-model="form.notes" rows="3" placeholder="Optional notes"></textarea>
          </div>
          <div class="field full">
            <label class="required">Upload Profile Image</label>
            <div class="file-row">
              <input ref="fileInput" type="file" accept="image/*" @change="handleAvatarUpload" />
              <button
                v-if="form.avatar"
                class="icon-button ghost"
                type="button"
                @click="handleAvatarRemove"
              >
                <v-icon icon="mdi-close" size="16" />
              </button>
            </div>
          </div>
          <div class="field full">
            <label class="required">Upload NRC Front</label>
            <div class="file-row">
              <input ref="nrcFrontInput" type="file" accept="image/*" @change="handleNrcFrontUpload" />
              <button
                v-if="form.nrcFront"
                class="icon-button ghost"
                type="button"
                @click="handleNrcFrontRemove"
              >
                <v-icon icon="mdi-close" size="16" />
              </button>
            </div>
          </div>
          <div class="field full">
            <label class="required">Upload NRC Back</label>
            <div class="file-row">
              <input ref="nrcBackInput" type="file" accept="image/*" @change="handleNrcBackUpload" />
              <button
                v-if="form.nrcBack"
                class="icon-button ghost"
                type="button"
                @click="handleNrcBackRemove"
              >
                <v-icon icon="mdi-close" size="16" />
              </button>
            </div>
          </div>
        </div>

        <p v-if="formError" class="form-error">{{ formError }}</p>
        <div class="dialog-actions">
          <button class="ghost" type="button" @click="close">Cancel</button>
          <button v-if="formStep > 1" class="ghost" type="button" @click="formStep -= 1">Back</button>
          <button v-if="formStep < formSteps.length" class="primary" type="button" @click="goNext">
            Next
          </button>
          <button v-else class="primary" type="submit">Add User</button>
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
  roles: {
    type: Array,
    default: () => []
  },
  departments: {
    type: Array,
    default: () => []
  }
})

const emit = defineEmits(['close', 'add'])

const roleOptions = computed(() => props.roles.length ? props.roles : ['Driver'])
const defaultRole = computed(() =>
  roleOptions.value.includes('Driver') ? 'Driver' : roleOptions.value[0] || 'Driver'
)
const departmentChoices = computed(() => props.departments.length ? props.departments : [])

const internalOpen = computed({
  get: () => props.open,
  set: (value) => {
    if (!value) emit('close')
  }
})

const form = reactive({
  name: '',
  employeeId: '',
  nrcNumber: '',
  email: '',
  role: defaultRole.value,
  status: 'Active',
  phone: '',
  avatar: '',
  nrcFront: '',
  nrcBack: '',
  department: '',
  title: '',
  location: '',
  manager: '',
  licenseNumber: '',
  licenseClass: '',
  licenseExpiry: '',
  emergencyContactName: '',
  emergencyContactRelation: '',
  emergencyContactPhone: '',
  address: '',
  twoFactorEnabled: false,
  notes: ''
})

const formStep = ref(1)
const formSteps = [
  { id: 1, title: 'Profile' },
  { id: 2, title: 'Work' },
  { id: 3, title: 'Contacts & Docs' }
]

const fileInput = ref(null)
const nrcFrontInput = ref(null)
const nrcBackInput = ref(null)
const formError = ref('')

const reset = () => {
  form.name = ''
  form.employeeId = ''
  form.nrcNumber = ''
  form.email = ''
  form.role = defaultRole.value
  form.status = 'Active'
  form.phone = ''
  form.avatar = ''
  form.nrcFront = ''
  form.nrcBack = ''
  form.department = ''
  form.title = ''
  form.location = ''
  form.manager = ''
  form.licenseNumber = ''
  form.licenseClass = ''
  form.licenseExpiry = ''
  form.emergencyContactName = ''
  form.emergencyContactRelation = ''
  form.emergencyContactPhone = ''
  form.address = ''
  form.twoFactorEnabled = false
  form.notes = ''
  if (fileInput.value) fileInput.value.value = ''
  if (nrcFrontInput.value) nrcFrontInput.value.value = ''
  if (nrcBackInput.value) nrcBackInput.value.value = ''
  formStep.value = 1
  formError.value = ''
}

watch(
  () => props.open,
  (value) => {
    if (value) reset()
  }
)

watch(
  roleOptions,
  (value) => {
    if (!value.includes(form.role)) {
      form.role = defaultRole.value
    }
  },
  { immediate: true }
)

const close = () => emit('close')

const submit = () => {
  const error = validate()
  if (error) {
    formError.value = error
    return
  }
  formError.value = ''
  emit('add', { ...form })
  reset()
}

const goNext = () => {
  const error = validateStep(formStep.value)
  if (error) {
    formError.value = error
    return
  }
  formError.value = ''
  formStep.value = Math.min(formStep.value + 1, formSteps.length)
}

const goToStep = (target) => {
  if (target <= formStep.value) {
    formStep.value = target
    formError.value = ''
    return
  }
  const error = validateStep(formStep.value)
  if (error) {
    formError.value = error
    return
  }
  formError.value = ''
  formStep.value = target
}

const NRC_PATTERN = /^\d{1,2}\/[A-Za-z]+\/\d{6}$/
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]{2,}$/
const PHONE_PATTERN = /^\+?[\d\s().-]{7,24}$/
const isValidNrc = (value) => NRC_PATTERN.test(value)
const isValidEmail = (value) => EMAIL_PATTERN.test(value)
const isValidPhone = (value) => {
  const digits = value.replace(/\D/g, '')
  return PHONE_PATTERN.test(value) && digits.length >= 7 && digits.length <= 15
}

const validate = () => {
  const step1 = validateStep(1)
  if (step1) return step1
  const step2 = validateStep(2)
  if (step2) return step2
  const step3 = validateStep(3)
  if (step3) return step3
  return ''
}

const validateStep = (step) => {
  if (step === 1) {
    if (!form.name) return 'Full name is required.'
    if (!form.employeeId) return 'Employee ID is required.'
    if (!form.nrcNumber) return 'NRC is required.'
    if (!isValidNrc(form.nrcNumber)) return 'NRC format must be like 9/ZaYaTha/111111.'
    if (!form.email) return 'Email is required.'
    if (!isValidEmail(form.email)) return 'Enter a valid email address.'
    if (!form.phone) return 'Phone number is required.'
    if (!isValidPhone(form.phone)) return 'Enter a valid phone number.'
    if (!form.role) return 'Role is required.'
    return ''
  }
  if (step === 2) {
    if (!form.title) return 'Job title is required.'
    if (!form.department) return 'Department is required.'
    if (!form.location) return 'Location is required.'
    if (!form.manager) return 'Manager is required.'
    if (form.role === 'Driver') {
      if (!form.licenseNumber) return 'License number is required for drivers.'
      if (!form.licenseClass) return 'License class is required for drivers.'
      if (!form.licenseExpiry) return 'License expiry is required for drivers.'
    }
    return ''
  }
  if (step === 3) {
    if (!form.emergencyContactName) return 'Emergency contact name is required.'
    if (!form.emergencyContactRelation) return 'Emergency contact relation is required.'
    if (!form.emergencyContactPhone) return 'Emergency contact phone is required.'
    if (!isValidPhone(form.emergencyContactPhone)) return 'Enter a valid emergency contact phone number.'
    if (!form.address) return 'Address is required.'
    if (!form.avatar) return 'Profile image is required.'
    if (!form.nrcFront) return 'NRC front image is required.'
    if (!form.nrcBack) return 'NRC back image is required.'
    return ''
  }
  return ''
}

const handleAvatarUpload = (event) => {
  const file = event.target.files?.[0]
  if (!file) return
  const reader = new FileReader()
  reader.onload = (e) => {
    form.avatar = e.target?.result || ''
  }
  reader.readAsDataURL(file)
}

const handleAvatarRemove = () => {
  form.avatar = ''
  if (fileInput.value) fileInput.value.value = ''
}

const handleNrcFrontUpload = (event) => {
  const file = event.target.files?.[0]
  if (!file) return
  const reader = new FileReader()
  reader.onload = (e) => {
    form.nrcFront = e.target?.result || ''
  }
  reader.readAsDataURL(file)
}

const handleNrcBackUpload = (event) => {
  const file = event.target.files?.[0]
  if (!file) return
  const reader = new FileReader()
  reader.onload = (e) => {
    form.nrcBack = e.target?.result || ''
  }
  reader.readAsDataURL(file)
}

const handleNrcFrontRemove = () => {
  form.nrcFront = ''
  if (nrcFrontInput.value) nrcFrontInput.value.value = ''
}

const handleNrcBackRemove = () => {
  form.nrcBack = ''
  if (nrcBackInput.value) nrcBackInput.value.value = ''
}
</script>

<style scoped src="./users_styles/AddUserDialog.css"></style>
