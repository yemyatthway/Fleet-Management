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
        <div class="field">
          <label>Full Name</label>
          <input v-model="form.name" type="text" placeholder="John Doe" required />
        </div>
        <div class="field">
          <label>Email Address</label>
          <input v-model="form.email" type="email" placeholder="john.doe@fleet.com" required />
        </div>
        <div class="field">
          <label>Phone Number</label>
          <input v-model="form.phone" type="tel" placeholder="+1 (555) 123-4567" required />
        </div>
        <div class="field">
          <label>Role</label>
          <select v-model="form.role" required>
            <option value="Driver">Driver</option>
            <option value="Dispatcher">Dispatcher</option>
            <option value="Mechanic">Mechanic</option>
            <option value="Admin">Admin</option>
          </select>
        </div>
        <div class="field">
          <label>Status</label>
          <select v-model="form.status" required>
            <option value="Active">Active</option>
            <option value="Disabled">Disabled</option>
          </select>
        </div>

        <div class="dialog-actions">
          <button class="ghost" type="button" @click="close">Cancel</button>
          <button class="primary" type="submit">Add User</button>
        </div>
      </form>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { computed, reactive, watch } from 'vue'

const props = defineProps({
  open: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['close', 'add'])

const internalOpen = computed({
  get: () => props.open,
  set: (value) => {
    if (!value) emit('close')
  }
})

const form = reactive({
  name: '',
  email: '',
  role: 'Driver',
  status: 'Active',
  phone: ''
})

const reset = () => {
  form.name = ''
  form.email = ''
  form.role = 'Driver'
  form.status = 'Active'
  form.phone = ''
}

watch(
  () => props.open,
  (value) => {
    if (value) reset()
  }
)

const close = () => emit('close')

const submit = () => {
  emit('add', { ...form })
  reset()
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
.field select {
  width: 100%;
  padding: 10px 12px;
  border-radius: 10px;
  border: 1px solid var(--fleet-border);
  font-size: 14px;
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
</style>
