<template>
  <div class="role-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">User Code Setup</h1>
        <p class="section-subtitle">Manage department and location master data used by user forms.</p>
      </div>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Codes</p>
        <h3>{{ totalCodes }}</h3>
      </div>
      <div class="stat-card">
        <p>Departments</p>
        <h3 class="text-info">{{ departmentCount }}</h3>
      </div>
      <div class="stat-card">
        <p>Locations</p>
        <h3 class="text-success">{{ locationCount }}</h3>
      </div>
      <div class="stat-card">
        <p>Active Codes</p>
        <h3 class="text-purple">{{ activeCount }}</h3>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input v-model="searchQuery" type="text" placeholder="Search code name or description..." />
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

        <div class="toolbar-filter">
          <v-icon icon="mdi-filter-variant" />
          <select v-model="activeType">
            <option value="All">All Types</option>
            <option value="Department">Department</option>
            <option value="Location">Location / Depot</option>
          </select>
        </div>

        <button class="primary-button" type="button" @click="openAdd">
          <v-icon icon="mdi-plus" size="18" />
          Add Code
        </button>
      </div>

      <div class="toolbar-count text-muted">
        {{ loadingOptions ? 'Loading code setup...' : `Showing ${options.length} of ${totalCodes} codes` }}
      </div>
    </div>

    <PageMessage
      :tone="pageMessage.tone"
      :title="pageMessage.title"
      :message="pageMessage.message"
      @close="clearPageMessage"
    />

    <UserCodeSetupTable
      :items="options"
      :total="totalCodes"
      :loading="loadingOptions"
      :items-per-page="tableOptions.itemsPerPage"
      @update:options="handleTableOptions"
      @edit="openEdit"
      @remove="handleDelete"
    />

    <UserCodeSetupDialog
      :open="dialogOpen"
      :mode="dialogMode"
      :item="selectedOption"
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
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import ConfirmDialog from '../common/ConfirmDialog.vue'
import PageMessage from '../common/PageMessage.vue'
import UserCodeSetupDialog from './UserCodeSetupDialog.vue'
import UserCodeSetupTable from './UserCodeSetupTable.vue'
import {
  createUserCodeOption,
  deleteUserCodeOption,
  getUserCodeOptions,
  updateUserCodeOption
} from '../../services/userCodeOptionsApi'

const ALL_TYPES = 'All'
const SEARCH_DELAY_MS = 350
const PAGE_MESSAGE_DURATION_MS = 5000

const useDebouncedRef = (source, delay = SEARCH_DELAY_MS) => {
  const debounced = ref(source.value)
  let timerId = null

  const clearTimer = () => {
    if (timerId) clearTimeout(timerId)
  }

  watch(
    source,
    (value) => {
      clearTimer()
      timerId = setTimeout(() => {
        debounced.value = value
      }, delay)
    },
    { immediate: true }
  )

  onBeforeUnmount(clearTimer)

  return debounced
}

const toRequest = (payload) => ({
  type: payload.type,
  name: payload.name,
  description: payload.description || null,
  status: payload.status || 'Active'
})

const options = ref([])
const searchQuery = ref('')
const activeType = ref(ALL_TYPES)
const totalCodes = ref(0)
const tableOptions = ref({ page: 1, itemsPerPage: 10 })
const pageMessage = ref({ tone: 'info', title: '', message: '' })
const loadingOptions = ref(false)
const dialogOpen = ref(false)
const dialogMode = ref('add')
const selectedOption = ref(null)
const confirmOpen = ref(false)
const confirmTitle = ref('Are you sure?')
const confirmMessage = ref('')
const confirmButton = ref('Confirm')
const confirmTone = ref('danger')
const pendingAction = ref(() => {})
let pageMessageTimerId = null

const debouncedQuery = useDebouncedRef(searchQuery)

const departmentCount = computed(() => options.value.filter((item) => item.type === 'Department').length)
const locationCount = computed(() => options.value.filter((item) => item.type === 'Location').length)
const activeCount = computed(() => options.value.filter((item) => item.status === 'Active').length)

const clearPageMessage = () => {
  if (pageMessageTimerId) {
    clearTimeout(pageMessageTimerId)
    pageMessageTimerId = null
  }
  pageMessage.value = { tone: 'info', title: '', message: '' }
}

const showPageMessage = ({ tone = 'info', title = '', message }) => {
  if (pageMessageTimerId) clearTimeout(pageMessageTimerId)
  pageMessage.value = { tone, title, message }
  pageMessageTimerId = setTimeout(() => {
    pageMessageTimerId = null
    clearPageMessage()
  }, PAGE_MESSAGE_DURATION_MS)
}

const loadOptions = async () => {
  loadingOptions.value = true
  clearPageMessage()

  try {
    const result = await getUserCodeOptions({
      page: tableOptions.value.page,
      pageSize: tableOptions.value.itemsPerPage,
      search: debouncedQuery.value,
      type: activeType.value === ALL_TYPES ? '' : activeType.value
    })
    options.value = result.items || []
    totalCodes.value = result.total || 0
  } catch (error) {
    showPageMessage({ tone: 'error', title: 'Could not load code setup', message: error.message })
  } finally {
    loadingOptions.value = false
  }
}

const handleTableOptions = (nextOptions) => {
  tableOptions.value = {
    page: nextOptions.page || 1,
    itemsPerPage: nextOptions.itemsPerPage || 10
  }
  loadOptions()
}

watch([debouncedQuery, activeType], () => {
  tableOptions.value.page = 1
  loadOptions()
})

const openAdd = () => {
  dialogMode.value = 'add'
  selectedOption.value = null
  dialogOpen.value = true
}

const openEdit = (item) => {
  dialogMode.value = 'edit'
  selectedOption.value = { ...item }
  dialogOpen.value = true
}

const handleSave = async (payload) => {
  clearPageMessage()
  const isEdit = dialogMode.value === 'edit'

  try {
    const savedItem = isEdit
      ? await updateUserCodeOption(payload.id, toRequest(payload))
      : await createUserCodeOption(toRequest(payload))

    if (isEdit) {
      options.value = options.value.map((item) => (item.id === savedItem.id ? savedItem : item))
    } else {
      await loadOptions()
    }

    dialogOpen.value = false
    showPageMessage({
      tone: 'success',
      title: isEdit ? 'Code updated' : 'Code created',
      message: `${savedItem.name} has been ${isEdit ? 'updated' : 'created'} successfully.`
    })
  } catch (error) {
    showPageMessage({ tone: 'error', title: 'Code was not saved', message: error.message })
  }
}

const openConfirm = ({ title, message, confirmText, tone, action }) => {
  confirmTitle.value = title
  confirmMessage.value = message
  confirmButton.value = confirmText
  confirmTone.value = tone
  pendingAction.value = action
  confirmOpen.value = true
}

const runConfirm = async () => {
  await pendingAction.value()
  confirmOpen.value = false
}

const handleDelete = (item) => {
  openConfirm({
    title: 'Delete Code?',
    message: `This will permanently remove ${item.name}.`,
    confirmText: 'Delete',
    tone: 'danger',
    action: async () => {
      clearPageMessage()

      try {
        await deleteUserCodeOption(item.id)
        await loadOptions()
        showPageMessage({
          tone: 'warning',
          title: 'Code deleted',
          message: `${item.name} has been removed.`
        })
      } catch (error) {
        showPageMessage({ tone: 'error', title: 'Code was not deleted', message: error.message })
      }
    }
  })
}

onMounted(loadOptions)

onBeforeUnmount(() => {
  if (pageMessageTimerId) clearTimeout(pageMessageTimerId)
})
</script>

<style scoped src="../roles/roles_styles/RoleManagementContent.css"></style>
