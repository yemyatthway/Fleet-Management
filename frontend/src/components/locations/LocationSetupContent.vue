<template>
  <div class="role-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Location / Depot Setup</h1>
        <p class="section-subtitle">Manage warehouse, depot, and hub master data for operations.</p>
      </div>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Locations</p>
        <h3>{{ totalLocations }}</h3>
      </div>
      <div class="stat-card">
        <p>Active Locations</p>
        <h3 class="text-info">{{ activeLocations }}</h3>
      </div>
      <div class="stat-card">
        <p>Disabled Locations</p>
        <h3 class="text-success">{{ disabledLocations }}</h3>
      </div>
      <div class="stat-card">
        <p>Recently Updated</p>
        <h3 class="text-purple">{{ recentlyUpdatedLocations }}</h3>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input v-model="searchQuery" type="text" placeholder="Search location name, code, city, or notes..." />
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

        <button class="primary-button" type="button" @click="openAdd">
          <v-icon icon="mdi-plus" size="18" />
          Add Location
        </button>
      </div>

      <div class="toolbar-count text-muted">
        {{ loadingLocations ? 'Loading locations...' : `Showing ${locations.length} of ${totalLocations} locations` }}
      </div>
    </div>

    <PageMessage
      :tone="pageMessage.tone"
      :title="pageMessage.title"
      :message="pageMessage.message"
      @close="clearPageMessage"
    />

    <LocationSetupTable
      :items="tableLocations"
      :total="totalLocations"
      :loading="loadingLocations"
      :page="tableOptions.page"
      :items-per-page="tableOptions.itemsPerPage"
      :sort-by="tableOptions.sortBy"
      :sort-order="tableOptions.sortOrder"
      @update:options="handleTableOptions"
      @edit="openEdit"
      @remove="handleDelete"
    />

    <LocationSetupDialog
      :open="dialogOpen"
      :mode="dialogMode"
      :item="selectedLocation"
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
import {
  createLocationCodeOption,
  deleteLocationCodeOption,
  getLocationCodeOptions,
  updateLocationCodeOption
} from '../../services/locationsApi'
import LocationSetupDialog from './LocationSetupDialog.vue'
import LocationSetupTable from './LocationSetupTable.vue'

const dialogOpen = ref(false)
const dialogMode = ref('add')
const selectedLocation = ref(null)
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
  items: locations,
  total: totalLocations,
  searchQuery,
  tableOptions,
  loading: loadingLocations,
  loadItems: loadLocations,
  handleTableOptions
} = useListPage({
  fetchPage: ({ page, pageSize, search, sortBy, sortOrder }) =>
    getLocationCodeOptions({ page, pageSize, search, sortBy, sortOrder }),
  clearPageMessage,
  showPageMessage,
  errorTitle: 'Could not load locations'
})
const {
  activeCount: activeLocations,
  disabledCount: disabledLocations,
  recentlyUpdatedCount: recentlyUpdatedLocations
} = useReferenceMetrics(locations)
const tableLocations = computed(() =>
  attachDisplayIds(
    locations.value,
    tableOptions.value.page,
    tableOptions.value.itemsPerPage,
    false,
    () => 'LOC',
    {
      total: totalLocations.value,
      sortBy: tableOptions.value.sortBy,
      sortOrder: tableOptions.value.sortOrder
    }
  )
)

const openAdd = () => {
  dialogMode.value = 'add'
  selectedLocation.value = { type: 'Warehouse', status: 'Active' }
  dialogOpen.value = true
}

const openEdit = (item) => {
  dialogMode.value = 'edit'
  selectedLocation.value = { ...item }
  dialogOpen.value = true
}

const handleSave = async (payload) => {
  clearPageMessage()
  const isEdit = dialogMode.value === 'edit'

  try {
    const savedLocation = isEdit
      ? await updateLocationCodeOption(payload.id, payload)
      : await createLocationCodeOption(payload)

    if (isEdit) {
      locations.value = locations.value.map((item) =>
        item.id === savedLocation.id ? savedLocation : item
      )
    } else {
      await loadLocations()
    }

    dialogOpen.value = false
    showPageMessage({
      tone: 'success',
      title: isEdit ? 'Location updated' : 'Location created',
      message: `${savedLocation.name} has been ${isEdit ? 'updated' : 'created'} successfully.`
    })
  } catch (error) {
    showPageMessage({ tone: 'error', title: 'Location was not saved', message: error.message })
  }
}

const handleDelete = (item) => {
  openConfirm({
    title: 'Delete Location?',
    message: `This will permanently remove ${item.name}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: async () => {
      clearPageMessage()

      try {
        await deleteLocationCodeOption(item.id)
        await loadLocations()
        showPageMessage({
          tone: 'warning',
          title: 'Location deleted',
          message: `${item.name} has been removed.`
        })
      } catch (error) {
        showPageMessage({ tone: 'error', title: 'Location was not deleted', message: error.message })
      }
    }
  })
}
</script>

<style scoped src="../roles/roles_styles/RoleManagementContent.css"></style>
