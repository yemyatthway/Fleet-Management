<template>
  <v-dialog v-model="internalOpen" max-width="520">
    <v-card class="dialog-card">
      <div class="dialog-header">
        <h2>{{ mode === 'edit' ? 'Edit Code' : 'Create Code' }}</h2>
        <button class="icon-button" type="button" @click="close">
          <v-icon icon="mdi-close" />
        </button>
      </div>

      <form class="dialog-body" @submit.prevent="submit">
        <div class="field">
          <label class="required">Type</label>
          <select v-model="form.type" required>
            <option value="Department">Department</option>
            <option value="Location">Location / Depot</option>
          </select>
        </div>
        <div class="field">
          <label class="required">Name</label>
          <input v-model="form.name" type="text" placeholder="Enter code name" required />
        </div>
        <div class="field">
          <label>Description</label>
          <textarea v-model="form.description" rows="3" placeholder="Optional description"></textarea>
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
          <button class="primary" type="submit">{{ mode === 'edit' ? 'Save Changes' : 'Create Code' }}</button>
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
  description: '',
  status: 'Active'
})

const formError = ref('')

const reset = () => {
  form.id = props.item?.id || ''
  form.type = props.item?.type || 'Department'
  form.name = props.item?.name || ''
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

  formError.value = ''
  emit('save', { ...form })
}
</script>

<style scoped src="../roles/roles_styles/RoleDialog.css"></style>
