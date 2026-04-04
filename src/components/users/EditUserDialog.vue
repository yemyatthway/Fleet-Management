<template>
  <v-dialog v-model="internalOpen" max-width="480">
    <v-card class="dialog-card">
      <div class="dialog-header">
        <h2>Edit User</h2>
        <button class="icon-button" type="button" @click="close">
          <v-icon icon="mdi-close" />
        </button>
      </div>

      <form class="dialog-body" @submit.prevent="submit">
        <div class="field">
          <label class="required">Full Name</label>
          <input v-model="form.name" type="text" placeholder="John Doe" required />
        </div>
        <div class="field">
          <label class="required">Employee ID</label>
          <input v-model="form.employeeId" type="text" placeholder="EMP-1021" required />
        </div>
        <div class="field">
          <label class="required">NRC</label>
          <div class="nrc-row">
            <select v-model="form.nrcState" required>
              <option v-for="code in nrcStateCodes" :key="code" :value="code">
                {{ code }}/
              </option>
            </select>
            <select v-model="form.nrcTownship" required>
              <option v-for="code in nrcTownships" :key="code" :value="code">
                {{ code }}
              </option>
            </select>
            <select v-model="form.nrcType" required>
              <option v-for="code in nrcTypes" :key="code" :value="code">
                ({{ code }})
              </option>
            </select>
            <input
              v-model="form.nrcSerial"
              type="text"
              inputmode="numeric"
              pattern="\\d{6}"
              maxlength="6"
              placeholder="123456"
              required
            />
          </div>
          <div class="nrc-preview text-muted">NRC: {{ nrcPreview }}</div>
        </div>
        <div class="field">
          <label class="required">Email Address</label>
          <input v-model="form.email" type="email" placeholder="john.doe@fleet.com" required />
        </div>
        <div class="field">
          <label class="required">Phone Number</label>
          <input v-model="form.phone" type="tel" placeholder="+1 (555) 123-4567" required />
        </div>
        <div class="field">
          <label class="required">Job Title</label>
          <input v-model="form.title" type="text" placeholder="Dispatcher" required />
        </div>
        <div class="field">
          <label class="required">Department</label>
          <input v-model="form.department" type="text" placeholder="Dispatch" required />
        </div>
        <div class="field">
          <label class="required">Location / Depot</label>
          <input v-model="form.location" type="text" placeholder="Central Hub" required />
        </div>
        <div class="field">
          <label class="required">Manager</label>
          <input v-model="form.manager" type="text" placeholder="Sarah Johnson" required />
        </div>
        <div class="field">
          <label class="required">Role</label>
          <select v-model="form.role" required>
            <option v-for="role in roleNames" :key="role" :value="role">
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
          <input v-model="form.emergencyContactPhone" type="tel" placeholder="+1 (555) 222-3344" required />
        </div>
        <div class="field">
          <label class="required">Address</label>
          <input v-model="form.address" type="text" placeholder="120 Market St, Springfield, IL" required />
        </div>
        <div class="field">
          <label>Last Login</label>
          <input v-model="form.lastLogin" type="datetime-local" disabled />
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
        <div class="field">
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
        <div class="field">
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
        <div class="field">
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

        <p v-if="formError" class="form-error">{{ formError }}</p>
        <div class="dialog-actions">
          <button class="ghost" type="button" @click="close">Cancel</button>
          <button class="primary" type="submit">Save Changes</button>
        </div>
      </form>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { computed, reactive, ref, watch } from 'vue'
import { roleNames } from '../../data/roles'

const props = defineProps({
  open: {
    type: Boolean,
    default: false
  },
  user: {
    type: Object,
    default: null
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
  employeeId: '',
  nrcState: '12',
  nrcTownship: 'ZaYaTha',
  nrcType: 'N',
  nrcSerial: '',
  email: '',
  role: roleNames.includes('Driver') ? 'Driver' : roleNames[0] || 'Driver',
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
  lastLogin: '',
  twoFactorEnabled: false,
  notes: ''
})

const fileInput = ref(null)
const nrcFrontInput = ref(null)
const nrcBackInput = ref(null)
const formError = ref('')

const reset = () => {
  form.id = props.user?.id || ''
  form.name = props.user?.name || ''
  form.employeeId = props.user?.employeeId || ''
  const parsed = parseNrc(props.user?.nrcNumber || '')
  form.nrcState = parsed.state
  form.nrcTownship = parsed.township
  form.nrcType = parsed.type
  form.nrcSerial = parsed.serial
  form.email = props.user?.email || ''
  form.role = props.user?.role || (roleNames.includes('Driver') ? 'Driver' : roleNames[0] || 'Driver')
  form.status = props.user?.status || 'Active'
  form.phone = props.user?.phone || ''
  form.avatar = props.user?.avatar || ''
  form.nrcFront = props.user?.nrcFront || ''
  form.nrcBack = props.user?.nrcBack || ''
  form.department = props.user?.department || ''
  form.title = props.user?.title || ''
  form.location = props.user?.location || ''
  form.manager = props.user?.manager || ''
  form.licenseNumber = props.user?.licenseNumber || ''
  form.licenseClass = props.user?.licenseClass || ''
  form.licenseExpiry = props.user?.licenseExpiry || ''
  form.emergencyContactName = props.user?.emergencyContactName || ''
  form.emergencyContactRelation = props.user?.emergencyContactRelation || ''
  form.emergencyContactPhone = props.user?.emergencyContactPhone || ''
  form.address = props.user?.address || ''
  form.lastLogin = props.user?.lastLogin ? props.user.lastLogin.replace('Z', '') : ''
  form.twoFactorEnabled = props.user?.twoFactorEnabled || false
  form.notes = props.user?.notes || ''
  if (fileInput.value) fileInput.value.value = ''
  if (nrcFrontInput.value) nrcFrontInput.value.value = ''
  if (nrcBackInput.value) nrcBackInput.value.value = ''
}

watch(
  () => props.open,
  (value) => {
    if (value) reset()
  }
)

const close = () => emit('close')

const submit = () => {
  const error = validate()
  if (error) {
    formError.value = error
    return
  }
  formError.value = ''
  emit('save', { ...form, nrcNumber: nrcPreview.value })
}

const nrcStateCodes = ['1', '2', '3', '4', '5', '6', '7', '8', '9', '10', '11', '12', '13', '14']
const nrcTownships = [
  'ZaYaTha',
  'KaMaNa',
  'MaYaTa',
  'PaKaNa',
  'BaMaNa',
  'DaPaYa',
  'LaMaNa',
  'SaKaNa'
]
const nrcTypes = ['N', 'E', 'P']

const nrcPreview = computed(() => {
  const serial = form.nrcSerial || '______'
  return `${form.nrcState}/${form.nrcTownship}(${form.nrcType})${serial}`
})

const parseNrc = (value) => {
  const match = value.match(/^(\d{1,2})\/(.+)\(([A-Z])\)(\d{6})$/)
  if (!match) {
    return { state: '12', township: 'ZaYaTha', type: 'N', serial: '' }
  }
  return { state: match[1], township: match[2], type: match[3], serial: match[4] }
}

const validate = () => {
  if (!form.name) return 'Full name is required.'
  if (!form.employeeId) return 'Employee ID is required.'
  if (!form.nrcSerial || form.nrcSerial.length !== 6) return 'NRC serial must be 6 digits.'
  if (!form.email) return 'Email is required.'
  if (!form.phone) return 'Phone number is required.'
  if (!form.title) return 'Job title is required.'
  if (!form.department) return 'Department is required.'
  if (!form.location) return 'Location is required.'
  if (!form.manager) return 'Manager is required.'
  if (form.role === 'Driver') {
    if (!form.licenseNumber) return 'License number is required for drivers.'
    if (!form.licenseClass) return 'License class is required for drivers.'
    if (!form.licenseExpiry) return 'License expiry is required for drivers.'
  }
  if (!form.emergencyContactName) return 'Emergency contact name is required.'
  if (!form.emergencyContactRelation) return 'Emergency contact relation is required.'
  if (!form.emergencyContactPhone) return 'Emergency contact phone is required.'
  if (!form.address) return 'Address is required.'
  if (!form.avatar) return 'Profile image is required.'
  if (!form.nrcFront) return 'NRC front image is required.'
  if (!form.nrcBack) return 'NRC back image is required.'
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

<style scoped>
.dialog-card {
  border-radius: 16px;
  padding: 0;
}

.dialog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px;
  border-bottom: 1px solid var(--fleet-border);
}

.dialog-header h2 {
  margin: 0;
  font-size: 18px;
}

.dialog-body {
  display: flex;
  flex-direction: column;
  gap: 14px;
  padding: 20px 24px 24px;
}

.field label {
  display: block;
  font-size: 12px;
  font-weight: 600;
  color: #334155;
  margin-bottom: 6px;
}

.field input,
.field select,
.field textarea {
  width: 100%;
  padding: 10px 12px;
  border-radius: 10px;
  border: 1px solid var(--fleet-border);
  font-size: 14px;
  font-family: inherit;
}

.nrc-row {
  display: grid;
  grid-template-columns: 90px 1fr 90px 140px;
  gap: 10px;
}

.nrc-preview {
  margin-top: 6px;
  font-size: 12px;
}

.dialog-actions {
  display: flex;
  gap: 12px;
  margin-top: 6px;
}

.dialog-actions button {
  flex: 1;
  border-radius: 10px;
  padding: 10px 12px;
  font-weight: 600;
  cursor: pointer;
  border: none;
}

.dialog-actions .ghost {
  background: #f8fafc;
  border: 1px solid var(--fleet-border);
  color: #334155;
}

.dialog-actions .primary {
  background: var(--fleet-primary);
  color: #fff;
}

.dialog-actions .primary:hover {
  background: var(--fleet-primary-dark);
}

.file-row {
  display: flex;
  align-items: center;
  gap: 10px;
}

.file-row input[type='file'] {
  flex: 1;
}

.icon-button.ghost {
  border: 1px solid var(--fleet-border);
  background: #f8fafc;
}

.icon-button.ghost:hover {
  background: #f1f5f9;
}

.icon-button {
  border: none;
  background: transparent;
  cursor: pointer;
  border-radius: 10px;
  width: 36px;
  height: 36px;
}

.icon-button:hover {
  background: #f1f5f9;
}

.checkbox-field label {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 13px;
  font-weight: 600;
  color: #334155;
}

.checkbox-field input[type='checkbox'] {
  width: 16px;
  height: 16px;
}

.required::after {
  content: ' *';
  color: #dc2626;
}

.form-error {
  margin: 6px 0 0;
  font-size: 12px;
  color: #dc2626;
}
</style>
