<template>
  <div class="role-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Department Setup</h1>
        <p class="section-subtitle">Manage department master data used by user forms.</p>
      </div>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Departments</p>
        <h3>{{ totalDepartments }}</h3>
      </div>
      <div class="stat-card">
        <p>Active Departments</p>
        <h3 class="text-info">{{ activeDepartments }}</h3>
      </div>
      <div class="stat-card">
        <p>Disabled Departments</p>
        <h3 class="text-success">{{ disabledDepartments }}</h3>
      </div>
      <div class="stat-card">
        <p>Recently Updated</p>
        <h3 class="text-purple">{{ recentlyUpdatedDepartments }}</h3>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input v-model="searchQuery" type="text" placeholder="Search department name or description..." />
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
          Add Department
        </button>
      </div>

      <div class="toolbar-count text-muted">
        {{ loadingDepartments ? 'Loading departments...' : `Showing ${departments.length} of ${totalDepartments} departments` }}
      </div>
    </div>

    <PageMessage
      :tone="pageMessage.tone"
      :title="pageMessage.title"
      :message="pageMessage.message"
      @close="clearPageMessage"
    />

    <DepartmentSetupTable
      :items="tableDepartments"
      :total="totalDepartments"
      :loading="loadingDepartments"
      :page="tableOptions.page"
      :items-per-page="tableOptions.itemsPerPage"
      :sort-by="tableOptions.sortBy"
      :sort-order="tableOptions.sortOrder"
      @update:options="handleTableOptions"
      @edit="openEdit"
      @remove="handleDelete"
    />

    <DepartmentSetupDialog
      :open="dialogOpen"
      :mode="dialogMode"
      :item="selectedDepartment"
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
import { buildReferenceRequest } from '../../utils/referenceData'
import {
  createDepartmentCodeOption,
  deleteDepartmentCodeOption,
  getDepartmentCodeOptions,
  updateDepartmentCodeOption
} from '../../services/departmentsApi'
import DepartmentSetupDialog from './DepartmentSetupDialog.vue'
import DepartmentSetupTable from './DepartmentSetupTable.vue'

const dialogOpen = ref(false)
const dialogMode = ref('add')
const selectedDepartment = ref(null)
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
  items: departments,
  total: totalDepartments,
  searchQuery,
  tableOptions,
  loading: loadingDepartments,
  loadItems: loadDepartments,
  handleTableOptions
} = useListPage({
  fetchPage: ({ page, pageSize, search, sortBy, sortOrder }) =>
    getDepartmentCodeOptions({ page, pageSize, search, sortBy, sortOrder }),
  clearPageMessage,
  showPageMessage,
  errorTitle: 'Could not load departments'
})
const {
  activeCount: activeDepartments,
  disabledCount: disabledDepartments,
  recentlyUpdatedCount: recentlyUpdatedDepartments
} = useReferenceMetrics(departments)
const tableDepartments = computed(() =>
  attachDisplayIds(
    departments.value,
    tableOptions.value.page,
    tableOptions.value.itemsPerPage,
    false,
    () => 'DEP',
    {
      total: totalDepartments.value,
      sortBy: tableOptions.value.sortBy,
      sortOrder: tableOptions.value.sortOrder
    }
  )
)

const openAdd = () => {
  dialogMode.value = 'add'
  selectedDepartment.value = { type: 'Department' }
  dialogOpen.value = true
}

const openEdit = (item) => {
  dialogMode.value = 'edit'
  selectedDepartment.value = { ...item }
  dialogOpen.value = true
}

const handleSave = async (payload) => {
  clearPageMessage()
  const isEdit = dialogMode.value === 'edit'

  try {
    const savedDepartment = isEdit
      ? await updateDepartmentCodeOption(payload.id, buildReferenceRequest('Department', payload))
      : await createDepartmentCodeOption(buildReferenceRequest('Department', payload))

    if (isEdit) {
      departments.value = departments.value.map((item) =>
        item.id === savedDepartment.id ? savedDepartment : item
      )
    } else {
      await loadDepartments()
    }

    dialogOpen.value = false
    showPageMessage({
      tone: 'success',
      title: isEdit ? 'Department updated' : 'Department created',
      message: `${savedDepartment.name} has been ${isEdit ? 'updated' : 'created'} successfully.`
    })
  } catch (error) {
    showPageMessage({ tone: 'error', title: 'Department was not saved', message: error.message })
  }
}

const handleDelete = (item) => {
  openConfirm({
    title: 'Delete Department?',
    message: `This will permanently remove ${item.name}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: async () => {
      clearPageMessage()

      try {
        await deleteDepartmentCodeOption(item.id)
        await loadDepartments()
        showPageMessage({
          tone: 'warning',
          title: 'Department deleted',
          message: `${item.name} has been removed.`
        })
      } catch (error) {
        showPageMessage({ tone: 'error', title: 'Department was not deleted', message: error.message })
      }
    }
  })
}
</script>

<style scoped src="../roles/roles_styles/RoleManagementContent.css"></style>
