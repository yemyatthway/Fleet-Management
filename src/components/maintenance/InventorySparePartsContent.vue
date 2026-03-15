<template>
  <div class="inventory-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Inventory & Spare Parts</h1>
        <p class="section-subtitle">Track stock levels, suppliers, and reorder points</p>
      </div>
      <button class="primary-button" type="button" @click="openPart">
        <v-icon icon="mdi-toolbox-outline" size="18" />
        Add Part
      </button>
    </div>

    <div class="card-surface section-card">
      <div class="section-header">
        <div>
          <div class="section-title">Parts Inventory</div>
          <div class="text-muted section-subtitle">Monitor stock and replenishment</div>
        </div>
      </div>
      <div class="table-wrap">
        <table class="table-base">
          <thead>
            <tr>
              <th>Part</th>
              <th>Part No.</th>
              <th>Category</th>
              <th>Stock</th>
              <th>Reorder</th>
              <th>Supplier</th>
              <th class="align-right">Unit Cost</th>
              <th class="align-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="part in parts" :key="part.id">
              <td>
                <strong>{{ part.name }}</strong>
                <div class="text-muted part-sub">{{ part.location }}</div>
              </td>
              <td class="text-muted">{{ part.partNo }}</td>
              <td>{{ part.category }}</td>
              <td>
                <span :class="part.stock <= part.reorderPoint ? 'text-warning' : ''">
                  {{ part.stock }}
                </span>
              </td>
              <td>{{ part.reorderPoint }}</td>
              <td>{{ part.supplier }}</td>
              <td class="align-right">{{ part.unitCost || '—' }}</td>
              <td class="align-right">
                <div class="inline-actions">
                  <button class="icon-button" type="button" @click="openPartDetails(part)">
                    <v-icon icon="mdi-eye-outline" size="18" />
                  </button>
                  <button class="icon-button" type="button" @click="openPartEdit(part)">
                    <v-icon icon="mdi-pencil-outline" size="18" />
                  </button>
                  <button class="icon-button danger" type="button" @click="deletePart(part.id)">
                    <v-icon icon="mdi-trash-can-outline" size="18" />
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-if="parts.length === 0" class="empty-state">
        No parts added yet
      </div>
    </div>

    <v-dialog v-model="partOpen" max-width="720">
      <div class="card-surface form-card">
        <div class="form-header">
          <div class="form-title">{{ partMode === 'edit' ? 'Edit Spare Part' : 'Add Spare Part' }}</div>
          <button class="icon-button" type="button" @click="partOpen = false">
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>

        <div v-if="partError" class="form-error">{{ partError }}</div>

        <div class="form-grid">
          <div class="form-field">
            <label>Part Name</label>
            <input v-model="partForm.name" type="text" placeholder="e.g., Brake Pads" />
          </div>
          <div class="form-field">
            <label>Part Number</label>
            <input v-model="partForm.partNo" type="text" placeholder="Part number" />
          </div>
          <div class="form-field">
            <label>Category</label>
            <input v-model="partForm.category" type="text" placeholder="e.g., Brakes" />
          </div>
          <div class="form-field">
            <label>Supplier</label>
            <input v-model="partForm.supplier" type="text" placeholder="Supplier name" />
          </div>
          <div class="form-field">
            <label>Stock On Hand</label>
            <input v-model="partForm.stock" type="number" min="0" placeholder="0" />
          </div>
          <div class="form-field">
            <label>Reorder Point</label>
            <input v-model="partForm.reorderPoint" type="number" min="0" placeholder="0" />
          </div>
          <div class="form-field">
            <label>Unit Cost</label>
            <input v-model="partForm.unitCost" type="text" placeholder="e.g., $42" />
          </div>
          <div class="form-field">
            <label>Location / Bin</label>
            <input v-model="partForm.location" type="text" placeholder="e.g., Bay 2 / Rack B" />
          </div>
        </div>

        <div class="form-actions">
          <button class="ghost-button" type="button" @click="partOpen = false">Cancel</button>
          <button class="primary-button" type="button" @click="savePart">
            {{ partMode === 'edit' ? 'Save Changes' : 'Save Part' }}
          </button>
        </div>
      </div>
    </v-dialog>

    <v-dialog v-model="partDetailsOpen" max-width="720">
      <div v-if="selectedPart" class="card-surface details-card">
        <div class="details-header">
          <div>
            <div class="details-title">{{ selectedPart.name }}</div>
            <div class="details-subtitle text-muted">{{ selectedPart.partNo }}</div>
          </div>
          <button class="icon-button" type="button" @click="partDetailsOpen = false">
            <v-icon icon="mdi-close" size="18" />
          </button>
        </div>
        <div class="details-grid">
          <div class="details-section">
            <h4>Stock</h4>
            <div class="details-row"><span>On Hand</span><strong>{{ selectedPart.stock }}</strong></div>
            <div class="details-row"><span>Reorder Point</span><strong>{{ selectedPart.reorderPoint }}</strong></div>
            <div class="details-row"><span>Unit Cost</span><strong>{{ selectedPart.unitCost || '—' }}</strong></div>
          </div>
          <div class="details-section">
            <h4>Supplier</h4>
            <div class="details-row"><span>Supplier</span><strong>{{ selectedPart.supplier || '—' }}</strong></div>
            <div class="details-row"><span>Category</span><strong>{{ selectedPart.category || '—' }}</strong></div>
            <div class="details-row"><span>Location</span><strong>{{ selectedPart.location || '—' }}</strong></div>
          </div>
        </div>
      </div>
    </v-dialog>

    <ConfirmDialog
      :open="confirmOpen"
      :title="confirmTitle"
      :message="confirmMessage"
      :confirm-text="confirmButton"
      :tone="confirmTone"
      @confirm="runConfirm"
      @cancel="confirmOpen = false"
    />
  </div>
</template>

<script setup>
import { ref } from 'vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'

const parts = ref([
  {
    id: 'PRT-4001',
    name: 'Brake Pads (Front)',
    partNo: 'BRK-FR-220',
    category: 'Brakes',
    stock: 18,
    reorderPoint: 8,
    supplier: 'Delta Auto Supply',
    unitCost: '$42',
    location: 'Bay 2 / Rack B'
  },
  {
    id: 'PRT-4015',
    name: 'Oil Filter',
    partNo: 'OIL-TR-120',
    category: 'Engine',
    stock: 6,
    reorderPoint: 10,
    supplier: 'Yangon Fleet Parts',
    unitCost: '$12',
    location: 'Bay 1 / Bin A'
  }
])

const partOpen = ref(false)
const partMode = ref('add')
const partError = ref('')
const partForm = ref({})
const partDetailsOpen = ref(false)
const selectedPart = ref(null)
const confirmOpen = ref(false)
const confirmTitle = ref('Are you sure?')
const confirmMessage = ref('')
const confirmButton = ref('Confirm')
const confirmTone = ref('danger')
const pendingAction = ref(() => {})

const buildEmptyPart = () => ({
  id: '',
  name: '',
  partNo: '',
  category: '',
  stock: 0,
  reorderPoint: 0,
  supplier: '',
  unitCost: '',
  location: ''
})

const openPart = () => {
  partMode.value = 'add'
  partForm.value = buildEmptyPart()
  partError.value = ''
  partOpen.value = true
}

const openPartEdit = (part) => {
  partMode.value = 'edit'
  partForm.value = { ...buildEmptyPart(), ...part }
  partError.value = ''
  partOpen.value = true
}

const openPartDetails = (part) => {
  selectedPart.value = part
  partDetailsOpen.value = true
}

const savePart = () => {
  if (!partForm.value.name || !partForm.value.partNo) {
    partError.value = 'Part name and part number are required.'
    return
  }
  if (partMode.value === 'add') {
    const newId = `PRT-${Math.floor(1000 + Math.random() * 9000)}`
    parts.value = [
      {
        ...partForm.value,
        id: newId
      },
      ...parts.value
    ]
  } else {
    parts.value = parts.value.map((item) =>
      item.id === partForm.value.id ? { ...item, ...partForm.value } : item
    )
  }
  partOpen.value = false
}

const openConfirm = ({ title, message, confirmText, tone, action }) => {
  confirmTitle.value = title
  confirmMessage.value = message
  confirmButton.value = confirmText
  confirmTone.value = tone
  pendingAction.value = action
  confirmOpen.value = true
}

const runConfirm = () => {
  pendingAction.value()
  confirmOpen.value = false
}

const deletePart = (id) => {
  const part = parts.value.find((item) => item.id === id)
  if (!part) return
  openConfirm({
    title: 'Delete Part?',
    message: `This will permanently remove ${part.name}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: () => {
      parts.value = parts.value.filter((item) => item.id !== id)
    }
  })
}
</script>

<style scoped>
.inventory-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
}

.section-card {
  padding: 18px 20px 22px;
}

.section-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 14px;
  border-bottom: 1px solid var(--fleet-border);
  margin-bottom: 14px;
}

.section-title {
  font-size: 18px;
  font-weight: 700;
}

.section-subtitle {
  font-size: 12px;
  margin-top: 4px;
}

.part-sub {
  font-size: 12px;
  margin-top: 2px;
}

.table-wrap {
  overflow-x: auto;
}

.align-right {
  text-align: right;
}

.empty-state {
  text-align: center;
  padding: 32px;
  color: var(--fleet-muted);
}

.primary-button {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border: none;
  border-radius: 12px;
  padding: 10px 16px;
  background: var(--fleet-primary);
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}

.primary-button:hover {
  background: var(--fleet-primary-dark);
}

.ghost-button {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border: 1px solid var(--fleet-border);
  border-radius: 12px;
  padding: 10px 16px;
  background: #fff;
  color: var(--fleet-text);
  font-weight: 600;
  cursor: pointer;
}

.ghost-button:hover {
  background: #f8fafc;
}

.form-card {
  padding: 18px 20px 22px;
  max-height: 80vh;
  overflow-y: auto;
}

.form-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  padding-bottom: 12px;
  border-bottom: 1px solid var(--fleet-border);
}

.form-title {
  font-weight: 700;
  font-size: 18px;
}

.form-error {
  margin-top: 12px;
  padding: 10px 12px;
  border-radius: 10px;
  background: #fee2e2;
  color: #b91c1c;
  font-size: 13px;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 12px 16px;
  margin-top: 16px;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  font-size: 13px;
}

.form-field label {
  color: var(--fleet-muted);
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.form-field input,
.form-field select {
  border: 1px solid var(--fleet-border);
  border-radius: 10px;
  padding: 9px 12px;
  font-size: 14px;
  background: #fff;
}

.form-field input:focus,
.form-field select:focus {
  outline: 2px solid rgba(37, 99, 235, 0.18);
  border-color: rgba(37, 99, 235, 0.6);
}

.form-actions {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  margin-top: 18px;
}

.details-card {
  padding: 20px 22px 24px;
}

.details-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--fleet-border);
}

.details-title {
  font-size: 18px;
  font-weight: 700;
}

.details-subtitle {
  font-size: 13px;
  margin-top: 4px;
}

.details-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 16px;
  padding-top: 18px;
}

.details-section {
  border: 1px solid var(--fleet-border);
  border-radius: 14px;
  padding: 14px;
  background: #fff;
}

.details-section h4 {
  margin: 0 0 10px;
  font-size: 14px;
}

.details-row {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  font-size: 13px;
  padding: 6px 0;
  border-bottom: 1px dashed #e2e8f0;
}

.details-row:last-child {
  border-bottom: none;
}

.icon-button {
  border: none;
  background: transparent;
  width: 34px;
  height: 34px;
  border-radius: 10px;
  cursor: pointer;
  color: #2563eb;
}

.icon-button:hover {
  background: #eff6ff;
}
</style>
