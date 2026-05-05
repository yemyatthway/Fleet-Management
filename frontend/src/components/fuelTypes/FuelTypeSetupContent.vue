<template>
  <div class="role-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Fuel Type Setup</h1>
        <p class="section-subtitle">Manage fuel type master data for vehicle assignment and maintenance workflows.</p>
      </div>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Fuel Types</p>
        <h3>{{ totalFuelTypes }}</h3>
      </div>
      <div class="stat-card">
        <p>Active Fuel Types</p>
        <h3 class="text-info">{{ activeFuelTypes }}</h3>
      </div>
      <div class="stat-card">
        <p>Disabled Fuel Types</p>
        <h3 class="text-success">{{ disabledFuelTypes }}</h3>
      </div>
      <div class="stat-card">
        <p>Recently Updated</p>
        <h3 class="text-purple">{{ recentlyUpdatedFuelTypes }}</h3>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input v-model="searchQuery" type="text" placeholder="Search fuel type name, code, or description..." />
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

        <button v-if="canCreateFuelTypes" class="primary-button" type="button" @click="openAdd">
          <v-icon icon="mdi-plus" size="18" />
          Add Fuel Type
        </button>
      </div>

      <div class="toolbar-count text-muted">
        {{ loadingFuelTypes ? 'Loading fuel types...' : `Showing ${fuelTypes.length} of ${totalFuelTypes} fuel types` }}
      </div>
    </div>

    <PageMessage
      :tone="pageMessage.tone"
      :title="pageMessage.title"
      :message="pageMessage.message"
      @close="clearPageMessage"
    />

    <FuelTypeSetupTable
      :items="tableFuelTypes"
      :total="totalFuelTypes"
      :loading="loadingFuelTypes"
      :page="tableOptions.page"
      :items-per-page="tableOptions.itemsPerPage"
      :sort-by="tableOptions.sortBy"
      :sort-order="tableOptions.sortOrder"
      :can-edit="canEditFuelTypes"
      :can-delete="canDeleteFuelTypes"
      @update:options="handleTableOptions"
      @edit="openEdit"
      @remove="handleDelete"
    />

    <FuelTypeSetupDialog
      :open="dialogOpen"
      :mode="dialogMode"
      :item="selectedFuelType"
      @close="dialogOpen = false"
      @save="handleSave"
    />

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
import { computed, ref } from 'vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'
import PageMessage from '../common/PageMessage.vue'
import { useConfirmDialog } from '../../composables/useConfirmDialog'
import { useListPage } from '../../composables/useListPage'
import { usePageMessage } from '../../composables/usePageMessage'
import { useReferenceMetrics } from '../../composables/useReferenceMetrics'
import { attachDisplayIds } from '../../utils/tableDisplayIds'
import { canCreateModule, canDeleteModule, canEditModule } from '../../utils/authSession'
import {
  createFuelTypeCodeOption,
  deleteFuelTypeCodeOption,
  getFuelTypeCodeOptions,
  updateFuelTypeCodeOption
} from '../../services/fuelTypesApi'
import FuelTypeSetupDialog from './FuelTypeSetupDialog.vue'
import FuelTypeSetupTable from './FuelTypeSetupTable.vue'

const dialogOpen = ref(false)
const dialogMode = ref('add')
const selectedFuelType = ref(null)
const canCreateFuelTypes = computed(() => canCreateModule('fuel-type-setup'))
const canEditFuelTypes = computed(() => canEditModule('fuel-type-setup'))
const canDeleteFuelTypes = computed(() => canDeleteModule('fuel-type-setup'))
const { pageMessage, clearPageMessage, showPageMessage } = usePageMessage()
const {
  confirmOpen,
  confirmTitle,
  confirmMessage,
  confirmButton,
  confirmTone,
  openConfirm,
  runConfirm
} = useConfirmDialog()
const {
  items: fuelTypes,
  total: totalFuelTypes,
  searchQuery,
  tableOptions,
  loading: loadingFuelTypes,
  loadItems: loadFuelTypes,
  handleTableOptions
} = useListPage({
  fetchPage: ({ page, pageSize, search, sortBy, sortOrder }) =>
    getFuelTypeCodeOptions({ page, pageSize, search, sortBy, sortOrder }),
  clearPageMessage,
  showPageMessage,
  errorTitle: 'Could not load fuel types'
})
const {
  activeCount: activeFuelTypes,
  disabledCount: disabledFuelTypes,
  recentlyUpdatedCount: recentlyUpdatedFuelTypes
} = useReferenceMetrics(fuelTypes)
const tableFuelTypes = computed(() =>
  attachDisplayIds(
    fuelTypes.value,
    tableOptions.value.page,
    tableOptions.value.itemsPerPage,
    false,
    () => 'FTP',
    {
      total: totalFuelTypes.value,
      sortBy: tableOptions.value.sortBy,
      sortOrder: tableOptions.value.sortOrder
    }
  )
)

const openAdd = () => {
  dialogMode.value = 'add'
  selectedFuelType.value = { status: 'Active' }
  dialogOpen.value = true
}

const openEdit = (item) => {
  dialogMode.value = 'edit'
  selectedFuelType.value = { ...item }
  dialogOpen.value = true
}

const resetTableOrder = () => {
  tableOptions.value = {
    ...tableOptions.value,
    page: 1,
    sortBy: 'id',
    sortOrder: 'asc'
  }
}

const handleSave = async (payload) => {
  clearPageMessage()
  const isEdit = dialogMode.value === 'edit'

  try {
    const savedFuelType = isEdit
      ? await updateFuelTypeCodeOption(payload.id, payload)
      : await createFuelTypeCodeOption(payload)

    resetTableOrder()
    await loadFuelTypes()

    dialogOpen.value = false
    showPageMessage({
      tone: 'success',
      title: isEdit ? 'Fuel type updated' : 'Fuel type created',
      message: `${savedFuelType.name} has been ${isEdit ? 'updated' : 'created'} successfully.`
    })
  } catch (error) {
    showPageMessage({ tone: 'error', title: 'Fuel type was not saved', message: error.message })
  }
}

const handleDelete = (item) => {
  openConfirm({
    title: 'Delete Fuel Type?',
    message: `This will permanently remove ${item.name}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: async () => {
      clearPageMessage()

      try {
        await deleteFuelTypeCodeOption(item.id)
        await loadFuelTypes()
        showPageMessage({
          tone: 'warning',
          title: 'Fuel type deleted',
          message: `${item.name} has been removed.`
        })
      } catch (error) {
        showPageMessage({ tone: 'error', title: 'Fuel type was not deleted', message: error.message })
      }
    }
  })
}
</script>

<style scoped src="../roles/roles_styles/RoleManagementContent.css"></style>
