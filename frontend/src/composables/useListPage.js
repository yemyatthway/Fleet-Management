import { onMounted, ref, watch } from 'vue'
import { useDebouncedRef } from './useDebouncedRef'

const DEFAULT_TABLE_OPTIONS = {
  page: 1,
  itemsPerPage: 10,
  sortBy: 'id',
  sortOrder: 'asc'
}

export function useListPage({
  fetchPage,
  clearPageMessage,
  showPageMessage,
  errorTitle = 'Could not load records',
  watchSources = [],
  initialItems = [],
  initialTotal = 0,
  initialTableOptions = DEFAULT_TABLE_OPTIONS,
  onLoaded,
  autoLoad = true
}) {
  const items = ref(initialItems)
  const total = ref(initialTotal)
  const searchQuery = ref('')
  const tableOptions = ref({ ...DEFAULT_TABLE_OPTIONS, ...initialTableOptions })
  const loading = ref(false)
  const debouncedSearchQuery = useDebouncedRef(searchQuery)

  const loadItems = async () => {
    loading.value = true
    clearPageMessage?.()

    try {
      const result = await fetchPage({
        page: tableOptions.value.page,
        pageSize: tableOptions.value.itemsPerPage,
        search: debouncedSearchQuery.value
      })

      items.value = result?.items || []
      total.value = result?.total || 0
      onLoaded?.(result)
    } catch (error) {
      showPageMessage?.({
        tone: 'error',
        title: errorTitle,
        message: error.message
      })
    } finally {
      loading.value = false
    }
  }

  const handleTableOptions = (options) => {
    tableOptions.value = {
      page: options.page || 1,
      itemsPerPage: options.itemsPerPage || 10,
      sortBy: 'id',
      sortOrder: 'asc'
    }
    loadItems()
  }

  watch([debouncedSearchQuery, ...watchSources], () => {
    tableOptions.value.page = 1
    loadItems()
  })

  if (autoLoad) onMounted(loadItems)

  return {
    items,
    total,
    searchQuery,
    debouncedSearchQuery,
    tableOptions,
    loading,
    loadItems,
    handleTableOptions
  }
}
