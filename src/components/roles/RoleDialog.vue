<template>
  <v-dialog v-model="internalOpen" max-width="520">
    <v-card class="dialog-card">
      <div class="dialog-header">
        <h2>{{ mode === 'edit' ? 'Edit Role' : 'Create Role' }}</h2>
        <button class="icon-button" type="button" @click="close">
          <v-icon icon="mdi-close" />
        </button>
      </div>

      <form class="dialog-body" @submit.prevent="submit">
        <div class="field">
          <label class="required">Role Name</label>
          <input v-model="form.name" type="text" placeholder="Role name" required />
        </div>
        <div v-if="mode === 'edit'" class="field">
          <label>Members</label>
          <input v-model.number="form.members" type="number" min="0" disabled />
        </div>
        <div class="field">
          <label>Description</label>
          <textarea v-model="form.description" rows="3" placeholder="Describe the role"></textarea>
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
          <button class="primary" type="submit">{{ mode === 'edit' ? 'Save Changes' : 'Create Role' }}</button>
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
  role: {
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
  description: '',
  members: 0,
  status: 'Active'
})

const formError = ref('')

const reset = () => {
  form.id = props.role?.id || ''
  form.name = props.role?.name || ''
  form.description = props.role?.description || ''
  form.members = props.role?.members ?? 0
  form.status = props.role?.status || 'Active'
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
  if (!form.name) {
    formError.value = 'Role name is required.'
    return
  }
  formError.value = ''
  emit('save', { ...form })
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
