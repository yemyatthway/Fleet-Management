<template>
  <DashboardLayout>
    <main class="records-page">
      <header class="records-header">
        <div>
          <h1>{{ title }}</h1>
          <p>{{ subtitle }}</p>
        </div>
        <button v-if="canCreate" class="primary-button" type="button" @click="startCreate">
          <v-icon icon="mdi-plus" size="20" />
          Add Document
        </button>
      </header>

      <section class="toolbar">
        <div class="search-box">
          <v-icon icon="mdi-magnify" size="22" />
          <input v-model="filters.search" placeholder="Search owner, document type, or number..." @input="loadRecords" />
        </div>
        <select v-model="filters.status" @change="loadRecords">
          <option value="">All Status</option>
          <option v-for="status in statusOptions" :key="status" :value="status">{{ status }}</option>
        </select>
      </section>

      <form v-if="showForm" class="record-form" @submit.prevent="saveRecord">
        <input v-model="form.ownerId" :placeholder="ownerType === 'Vehicle' ? 'Vehicle/ID' : 'Employee ID'" required />
        <input v-model="form.ownerName" :placeholder="ownerType === 'Vehicle' ? 'Vehicle name' : 'Driver name'" required />
        <select v-model="form.documentType" required>
          <option value="" disabled>Document type</option>
          <option v-for="type in documentTypeOptions" :key="type" :value="type">{{ type }}</option>
        </select>
        <input v-model="form.documentNumber" placeholder="Document number" />
        <input v-model="form.issueDate" type="date" />
        <input v-model="form.expiryDate" type="date" />
        <select v-model="form.status" required>
          <option v-for="status in statusOptions" :key="status" :value="status">{{ status }}</option>
        </select>
        <input v-model="form.notes" placeholder="Notes" />
        <div class="form-actions">
          <button class="ghost-button" type="button" @click="cancelForm">Cancel</button>
          <button class="primary-button" type="submit">{{ editingId ? 'Save Document' : 'Create Document' }}</button>
        </div>
      </form>

      <section class="table-card">
        <table>
          <thead>
            <tr>
              <th>No.</th>
              <th>Owner</th>
              <th>Document Type</th>
              <th>Number</th>
              <th>Issue Date</th>
              <th>Expiry Date</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(document, index) in records" :key="document.id">
              <td>{{ index + 1 }}</td>
              <td>
                <strong>{{ document.ownerId }}</strong>
                <span>{{ document.ownerName }}</span>
              </td>
              <td>{{ document.documentType }}</td>
              <td>{{ document.documentNumber || '-' }}</td>
              <td>{{ document.issueDate || '-' }}</td>
              <td>{{ document.expiryDate || '-' }}</td>
              <td><span class="status-pill">{{ document.status }}</span></td>
              <td>
                <div class="row-actions">
                  <button v-if="canEdit" type="button" @click="startEdit(document)"><v-icon icon="mdi-pencil" size="18" /></button>
                  <button v-if="canDelete" type="button" class="danger" @click="removeRecord(document.id)"><v-icon icon="mdi-delete-outline" size="18" /></button>
                </div>
              </td>
            </tr>
            <tr v-if="!records.length">
              <td colspan="8" class="empty-cell">No document records found</td>
            </tr>
          </tbody>
        </table>
      </section>
    </main>
  </DashboardLayout>
</template>

<script setup>
import { computed, onMounted, reactive, ref, watch } from 'vue'
import DashboardLayout from '../layouts/DashboardLayout.vue'
import { createDocument, deleteDocument, getDocuments, updateDocument } from '../services/documentsApi'
import { documentTypesApi, statusesApi } from '../services/tripSetupApi'
import { canCreateModule, canDeleteModule, canEditModule } from '../utils/authSession'

const props = defineProps({
  ownerType: { type: String, required: true }
})

const moduleKey = computed(() => (props.ownerType === 'Driver' ? 'driver-documents' : 'vehicle-documents'))
const title = computed(() => (props.ownerType === 'Driver' ? 'Driver Documents' : 'Vehicle Documents'))
const subtitle = computed(() => (props.ownerType === 'Driver' ? 'Track driver license, NRC, and profile document expiry.' : 'Track registration, insurance, road tax, and inspection expiry.'))
const records = ref([])
const showForm = ref(false)
const editingId = ref(null)
const documentTypeOptions = ref(['Registration', 'Insurance', 'Road Tax', 'Driver License', 'NRC'])
const statusOptions = ref(['Active', 'Pending', 'Expired', 'Renewed'])
const filters = reactive({ search: '', status: '' })
const form = reactive({ ownerType: props.ownerType, ownerId: '', ownerName: '', documentType: '', documentNumber: '', issueDate: '', expiryDate: '', status: 'Active', notes: '' })

const canCreate = computed(() => canCreateModule(moduleKey.value))
const canEdit = computed(() => canEditModule(moduleKey.value))
const canDelete = computed(() => canDeleteModule(moduleKey.value))

const loadOptions = async () => {
  try {
    const [types, statuses] = await Promise.all([documentTypesApi.options(), statusesApi.options()])
    if (types?.length) documentTypeOptions.value = types
    if (statuses?.length) statusOptions.value = statuses
  } catch (error) {
    console.error(error)
  }
}

const loadRecords = async () => {
  const result = await getDocuments({ ...filters, ownerType: props.ownerType, pageSize: 100 })
  records.value = result?.items || []
}

const resetForm = () => {
  Object.assign(form, { ownerType: props.ownerType, ownerId: '', ownerName: '', documentType: documentTypeOptions.value[0] || '', documentNumber: '', issueDate: '', expiryDate: '', status: statusOptions.value[0] || 'Active', notes: '' })
  editingId.value = null
}

const startCreate = () => {
  resetForm()
  showForm.value = true
}

const startEdit = (document) => {
  Object.assign(form, { ...document, notes: document.notes || '', documentNumber: document.documentNumber || '', issueDate: document.issueDate || '', expiryDate: document.expiryDate || '' })
  editingId.value = document.id
  showForm.value = true
}

const cancelForm = () => {
  showForm.value = false
  resetForm()
}

const saveRecord = async () => {
  if (editingId.value) await updateDocument(editingId.value, form)
  else await createDocument(form)
  showForm.value = false
  resetForm()
  await loadRecords()
}

const removeRecord = async (id) => {
  await deleteDocument(id, props.ownerType)
  await loadRecords()
}

watch(() => props.ownerType, async () => {
  resetForm()
  await loadRecords()
})

onMounted(async () => {
  await loadOptions()
  resetForm()
  await loadRecords()
})
</script>

<style scoped>
.records-page { padding: 28px 32px; display: grid; gap: 20px; }
.records-header { display: flex; align-items: center; justify-content: space-between; gap: 16px; }
h1 { margin: 0; font-size: 28px; }
p { margin: 6px 0 0; color: #64748b; }
.toolbar, .record-form, .table-card { background: #fff; border: 1px solid #dfe3ea; border-radius: 16px; padding: 18px; }
.toolbar { display: grid; grid-template-columns: minmax(260px, 1fr) 180px; gap: 14px; }
.search-box { display: flex; align-items: center; gap: 10px; }
input, select { width: 100%; min-height: 44px; border: 1px solid #dfe3ea; border-radius: 10px; padding: 0 12px; font: inherit; background: #fff; }
.record-form { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.form-actions { display: flex; justify-content: flex-end; gap: 10px; grid-column: 1 / -1; }
.primary-button, .ghost-button { min-height: 44px; border: 0; border-radius: 10px; padding: 0 18px; font-weight: 700; display: inline-flex; align-items: center; justify-content: center; gap: 8px; cursor: pointer; }
.primary-button { background: #2563eb; color: white; }
.ghost-button { background: #eef2f7; color: #334155; }
table { width: 100%; border-collapse: collapse; }
th, td { padding: 14px; text-align: left; border-bottom: 1px solid #e5e7eb; vertical-align: top; }
td span { display: block; margin-top: 4px; color: #64748b; }
th { color: #475569; font-weight: 700; }
.status-pill { display: inline-flex; padding: 5px 12px; border-radius: 999px; background: #e0f2fe; color: #0369a1; font-weight: 700; }
.row-actions { display: flex; gap: 10px; }
.row-actions button { border: 0; background: transparent; color: #2563eb; cursor: pointer; }
.row-actions .danger { color: #dc2626; }
.empty-cell { text-align: center; color: #64748b; padding: 48px; }
@media (max-width: 900px) { .toolbar, .record-form { grid-template-columns: 1fr; } .records-header { align-items: stretch; flex-direction: column; } }
</style>
