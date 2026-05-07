<template>
  <div class="role-page inventory-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Inventory & Spare Parts</h1>
        <p class="section-subtitle">Track stock levels, suppliers, and reorder points.</p>
      </div>
    </div>

    <PageMessage
      :tone="pageMessage.tone"
      :title="pageMessage.title"
      :message="pageMessage.message"
      @close="clearPageMessage"
    />

    <div v-if="pageError" class="form-error">{{ pageError }}</div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Parts</p>
        <h3>{{ parts.length }}</h3>
        <span class="stat-foot text-muted">Catalog items</span>
      </div>
      <div class="stat-card">
        <p>Low Stock</p>
        <h3 class="text-warning">{{ lowStockCount }}</h3>
        <span class="stat-foot text-muted">Needs reorder</span>
      </div>
      <div class="stat-card">
        <p>Categories</p>
        <h3 class="text-info">{{ categoryOptions.length }}</h3>
        <span class="stat-foot text-muted">Inventory groups</span>
      </div>
      <div class="stat-card">
        <p>Stock Value</p>
        <h3>{{ inventoryValue }}</h3>
        <span class="stat-foot text-muted">On hand estimate</span>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search part, number, supplier, or bin..."
          />
          <button
            v-if="searchQuery"
            class="clear-button"
            type="button"
            aria-label="Clear search"
            @click="searchQuery = ''"
          >
            <v-icon icon="mdi-close-circle" size="18" />
          </button>
        </div>

        <div class="toolbar-actions">
          <div class="toolbar-filter">
            <v-icon icon="mdi-shape-outline" />
            <select v-model="categoryFilter">
              <option value="All">All Categories</option>
              <option v-for="category in categoryOptions" :key="category" :value="category">
                {{ category }}
              </option>
            </select>
          </div>

          <div class="toolbar-filter">
            <v-icon icon="mdi-filter-variant" />
            <select v-model="stockFilter">
              <option value="All">All Stock</option>
              <option value="Low">Low Stock</option>
              <option value="Healthy">Healthy Stock</option>
            </select>
          </div>

          <button v-if="canCreate" class="primary-button" type="button" @click="openPart">
            <v-icon icon="mdi-toolbox-outline" size="18" />
            Add Part
          </button>
        </div>
      </div>

      <div class="toolbar-count text-muted">
        Showing {{ filteredParts.length }} of {{ parts.length }} parts
      </div>
    </div>

    <div class="card-surface table-card">
      <div class="table-wrap">
        <v-data-table
          class="table-base inventory-table"
          :headers="partHeaders"
          :items="filteredParts"
          :loading="loading"
          :items-per-page="10"
          :items-per-page-options="[10, 20, 30]"
          :mobile-breakpoint="0"
          :mobile="false"
          fixed-header
          height="520"
          density="comfortable"
        >
          <template #item.part="{ item }">
            <div class="part-cell">
              <img v-if="item.image" class="part-image" :src="item.image" :alt="item.name" />
              <span v-else class="part-avatar">
                <v-icon icon="mdi-cog-outline" size="18" />
              </span>
              <div>
                <strong>{{ item.name }}</strong>
                <div class="text-muted part-sub">{{ item.location }}</div>
              </div>
            </div>
          </template>

          <template #item.partNo="{ item }">
            <span class="text-muted">{{ item.partNo }}</span>
          </template>

          <template #item.stock="{ item }">
            <span class="role-badge" :class="stockClass(item)">
              {{ item.stock }} on hand
            </span>
          </template>

          <template #item.reorderPoint="{ item }">
            <span class="text-muted">{{ item.reorderPoint }}</span>
          </template>

          <template #item.unitCost="{ item }">
            <span>{{ item.unitCost || '—' }}</span>
          </template>

          <template #item.actions="{ item }">
            <div class="inline-actions">
              <button class="icon-button tooltip" type="button" @click="openPartDetails(item)">
                <v-icon icon="mdi-eye-outline" size="18" />
                <span class="tooltip-text">View details</span>
              </button>
              <button v-if="canEdit" class="icon-button tooltip" type="button" @click="openPartEdit(item)">
                <v-icon icon="mdi-pencil-outline" size="18" />
                <span class="tooltip-text">Edit part</span>
              </button>
              <button v-if="canDelete" class="icon-button danger tooltip" type="button" @click="deletePart(item.id)">
                <v-icon icon="mdi-trash-can-outline" size="18" />
                <span class="tooltip-text">Delete part</span>
              </button>
            </div>
          </template>

          <template #no-data>
            <div class="empty-state">No parts found matching your criteria</div>
          </template>
        </v-data-table>
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
            <select v-model="partForm.supplier">
              <option value="">Select supplier</option>
              <option v-for="supplier in supplierOptions" :key="supplier" :value="supplier">{{ supplier }}</option>
            </select>
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
          <div class="form-field">
            <label>Part Image</label>
            <input type="file" accept="image/*" @change="handleImageChange" />
          </div>
          <label v-if="partForm.image" class="check-field">
            <input v-model="partForm.removeImage" type="checkbox" />
            Remove existing image
          </label>
        </div>

        <div class="form-actions">
          <button class="ghost-button" type="button" @click="partOpen = false">Cancel</button>
          <button class="primary-button" type="button" :disabled="saving" @click="savePart">
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
import { computed, onMounted, ref } from 'vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'
import PageMessage from '../common/PageMessage.vue'
import { usePageMessage } from '../../composables/usePageMessage'
import { createInventoryPart, deleteInventoryPart, getInventoryParts, updateInventoryPart } from '../../services/inventoryPartsApi'
import { suppliersApi } from '../../services/tripSetupApi'
import { canCreateModule, canDeleteModule, canEditModule } from '../../utils/authSession'

const moduleKey = 'inventory-parts'
const parts = ref([])
const loading = ref(false)
const saving = ref(false)
const pageError = ref('')
const { pageMessage, clearPageMessage, showPageMessage } = usePageMessage(4000)
const supplierOptions = ref([])
const canCreate = computed(() => canCreateModule(moduleKey))
const canEdit = computed(() => canEditModule(moduleKey))
const canDelete = computed(() => canDeleteModule(moduleKey))

const searchQuery = ref('')
const categoryFilter = ref('All')
const stockFilter = ref('All')
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

const partHeaders = [
  { title: 'Part', key: 'part', sortable: false },
  { title: 'Part No.', key: 'partNo', sortable: false },
  { title: 'Category', key: 'category', sortable: false },
  { title: 'Stock', key: 'stock', sortable: false },
  { title: 'Reorder', key: 'reorderPoint', sortable: false },
  { title: 'Supplier', key: 'supplier', sortable: false },
  { title: 'Unit Cost', key: 'unitCost', align: 'end', sortable: false },
  { title: 'Actions', key: 'actions', align: 'end', sortable: false }
]

const categoryOptions = computed(() =>
  [...new Set(parts.value.map((part) => part.category).filter(Boolean))].sort()
)

const lowStockCount = computed(() =>
  parts.value.filter((part) => Number(part.stock) <= Number(part.reorderPoint)).length
)

const inventoryValue = computed(() => {
  const value = parts.value.reduce((total, part) => {
    const unitCost = Number(String(part.unitCost || '').replace(/[^0-9.]/g, '')) || 0
    return total + unitCost * Number(part.stock || 0)
  }, 0)

  return value.toLocaleString('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0
  })
})

const filteredParts = computed(() => {
  const query = searchQuery.value.trim().toLowerCase()

  return parts.value.filter((part) => {
    const matchesSearch = !query || [
      part.name,
      part.partNo,
      part.category,
      part.supplier,
      part.location
    ].some((value) => String(value || '').toLowerCase().includes(query))

    const matchesCategory = categoryFilter.value === 'All' || part.category === categoryFilter.value
    const isLowStock = Number(part.stock) <= Number(part.reorderPoint)
    const matchesStock =
      stockFilter.value === 'All' ||
      (stockFilter.value === 'Low' && isLowStock) ||
      (stockFilter.value === 'Healthy' && !isLowStock)

    return matchesSearch && matchesCategory && matchesStock
  })
})

const stockClass = (part) =>
  Number(part.stock) <= Number(part.reorderPoint) ? 'role-mechanic' : 'role-driver'

const buildEmptyPart = () => ({
  id: '',
  name: '',
  partNo: '',
  category: '',
  stock: 0,
  reorderPoint: 0,
  supplier: '',
  unitCost: '',
  location: '',
  image: '',
  imageFile: null,
  removeImage: false
})

const loadParts = async () => {
  loading.value = true
  pageError.value = ''
  try {
    parts.value = await getInventoryParts()
  } catch (error) {
    pageError.value = error.message || 'Could not load inventory parts.'
  } finally {
    loading.value = false
  }
}

const loadSuppliers = async () => {
  try {
    supplierOptions.value = await suppliersApi.options()
  } catch (error) {
    console.error(error)
  }
}

const openPart = () => {
  partMode.value = 'add'
  partForm.value = buildEmptyPart()
  partError.value = ''
  partOpen.value = true
}

const openPartEdit = (part) => {
  partMode.value = 'edit'
  partForm.value = { ...buildEmptyPart(), ...part, imageFile: null, removeImage: false }
  partError.value = ''
  partOpen.value = true
}

const openPartDetails = (part) => {
  selectedPart.value = part
  partDetailsOpen.value = true
}

const savePart = async () => {
  if (!partForm.value.name || !partForm.value.partNo) {
    partError.value = 'Part name and part number are required.'
    showPageMessage({
      tone: 'error',
      title: 'Part was not saved',
      message: partError.value
    })
    return
  }
  saving.value = true
  partError.value = ''
  pageError.value = ''
  try {
    const isEdit = partMode.value === 'edit'
    if (partMode.value === 'add') {
      await createInventoryPart(partForm.value)
    } else {
      await updateInventoryPart(partForm.value.id, partForm.value)
    }
    partOpen.value = false
    await loadParts()
    showPageMessage({
      tone: 'success',
      title: isEdit ? 'Part updated' : 'Part created',
      message: isEdit ? 'Inventory part was updated successfully.' : 'Inventory part was created successfully.'
    })
  } catch (error) {
    partError.value = error.message || 'Could not save part.'
    showPageMessage({
      tone: 'error',
      title: 'Part was not saved',
      message: partError.value
    })
  } finally {
    saving.value = false
  }
}

const handleImageChange = (event) => {
  partForm.value.imageFile = event.target.files?.[0] || null
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
    action: async () => {
      try {
        pageError.value = ''
        await deleteInventoryPart(id)
        await loadParts()
        showPageMessage({
          tone: 'success',
          title: 'Part deleted',
          message: `${part.name} was deleted successfully.`
        })
      } catch (error) {
        pageError.value = error.message || 'Could not delete part.'
        showPageMessage({
          tone: 'error',
          title: 'Part was not deleted',
          message: pageError.value
        })
      }
    }
  })
}

onMounted(async () => {
  await Promise.all([loadParts(), loadSuppliers()])
})
</script>

<style scoped src="../roles/roles_styles/RoleManagementContent.css"></style>
<style scoped src="../roles/roles_styles/RoleTable.css"></style>

<style scoped src="./maintenance_styles/InventorySparePartsContent.css"></style>
