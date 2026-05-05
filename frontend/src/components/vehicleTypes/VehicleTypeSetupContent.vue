<template>
  <div class="role-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Vehicle Type Setup</h1>
        <p class="section-subtitle">Manage vehicle type master data used across fleet operations.</p>
      </div>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Vehicle Types</p>
        <h3>{{ totalVehicleTypes }}</h3>
      </div>
      <div class="stat-card">
        <p>Active Vehicle Types</p>
        <h3 class="text-info">{{ activeVehicleTypes }}</h3>
      </div>
      <div class="stat-card">
        <p>Disabled Vehicle Types</p>
        <h3 class="text-success">{{ disabledVehicleTypes }}</h3>
      </div>
      <div class="stat-card">
        <p>Recently Updated</p>
        <h3 class="text-purple">{{ recentlyUpdatedVehicleTypes }}</h3>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input v-model="searchQuery" type="text" placeholder="Search vehicle type name, code, or description..." />
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

        <button v-if="canCreateVehicleTypes" class="primary-button" type="button" @click="openAdd">
          <v-icon icon="mdi-plus" size="18" />
          Add Vehicle Type
        </button>
      </div>

      <div class="toolbar-count text-muted">
        {{ loadingVehicleTypes ? 'Loading vehicle types...' : `Showing ${vehicleTypes.length} of ${totalVehicleTypes} vehicle types` }}
      </div>
    </div>

    <PageMessage
      :tone="pageMessage.tone"
      :title="pageMessage.title"
      :message="pageMessage.message"
      @close="clearPageMessage"
    />

    <VehicleTypeSetupTable
      :items="tableVehicleTypes"
      :total="totalVehicleTypes"
      :loading="loadingVehicleTypes"
      :page="tableOptions.page"
      :items-per-page="tableOptions.itemsPerPage"
      :sort-by="tableOptions.sortBy"
      :sort-order="tableOptions.sortOrder"
      :can-edit="canEditVehicleTypes"
      :can-delete="canDeleteVehicleTypes"
      @update:options="handleTableOptions"
      @edit="openEdit"
      @remove="handleDelete"
    />

    <VehicleTypeSetupDialog
      :open="dialogOpen"
      :mode="dialogMode"
      :item="selectedVehicleType"
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
  createVehicleTypeCodeOption,
  deleteVehicleTypeCodeOption,
  getVehicleTypeCodeOptions,
  updateVehicleTypeCodeOption
} from '../../services/vehicleTypesApi'
import VehicleTypeSetupDialog from './VehicleTypeSetupDialog.vue'
import VehicleTypeSetupTable from './VehicleTypeSetupTable.vue'

const dialogOpen = ref(false)
const dialogMode = ref('add')
const selectedVehicleType = ref(null)
const canCreateVehicleTypes = computed(() => canCreateModule('vehicle-type-setup'))
const canEditVehicleTypes = computed(() => canEditModule('vehicle-type-setup'))
const canDeleteVehicleTypes = computed(() => canDeleteModule('vehicle-type-setup'))
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
  items: vehicleTypes,
  total: totalVehicleTypes,
  searchQuery,
  tableOptions,
  loading: loadingVehicleTypes,
  loadItems: loadVehicleTypes,
  handleTableOptions
} = useListPage({
  fetchPage: ({ page, pageSize, search, sortBy, sortOrder }) =>
    getVehicleTypeCodeOptions({ page, pageSize, search, sortBy, sortOrder }),
  clearPageMessage,
  showPageMessage,
  errorTitle: 'Could not load vehicle types'
})
const {
  activeCount: activeVehicleTypes,
  disabledCount: disabledVehicleTypes,
  recentlyUpdatedCount: recentlyUpdatedVehicleTypes
} = useReferenceMetrics(vehicleTypes)
const tableVehicleTypes = computed(() =>
  attachDisplayIds(
    vehicleTypes.value,
    tableOptions.value.page,
    tableOptions.value.itemsPerPage,
    false,
    () => 'VTP',
    {
      total: totalVehicleTypes.value,
      sortBy: tableOptions.value.sortBy,
      sortOrder: tableOptions.value.sortOrder
    }
  )
)

const openAdd = () => {
  dialogMode.value = 'add'
  selectedVehicleType.value = { status: 'Active' }
  dialogOpen.value = true
}

const openEdit = (item) => {
  dialogMode.value = 'edit'
  selectedVehicleType.value = { ...item }
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
    const savedVehicleType = isEdit
      ? await updateVehicleTypeCodeOption(payload.id, payload)
      : await createVehicleTypeCodeOption(payload)

    resetTableOrder()
    await loadVehicleTypes()

    dialogOpen.value = false
    showPageMessage({
      tone: 'success',
      title: isEdit ? 'Vehicle type updated' : 'Vehicle type created',
      message: `${savedVehicleType.name} has been ${isEdit ? 'updated' : 'created'} successfully.`
    })
  } catch (error) {
    showPageMessage({ tone: 'error', title: 'Vehicle type was not saved', message: error.message })
  }
}

const handleDelete = (item) => {
  openConfirm({
    title: 'Delete Vehicle Type?',
    message: `This will permanently remove ${item.name}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: async () => {
      clearPageMessage()

      try {
        await deleteVehicleTypeCodeOption(item.id)
        await loadVehicleTypes()
        showPageMessage({
          tone: 'warning',
          title: 'Vehicle type deleted',
          message: `${item.name} has been removed.`
        })
      } catch (error) {
        showPageMessage({ tone: 'error', title: 'Vehicle type was not deleted', message: error.message })
      }
    }
  })
}
</script>

<style scoped src="../roles/roles_styles/RoleManagementContent.css"></style>
