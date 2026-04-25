import { computed } from 'vue'

export function useReferenceMetrics(items) {
  const activeCount = computed(() => items.value.filter((item) => item.status === 'Active').length)
  const disabledCount = computed(() => items.value.filter((item) => item.status !== 'Active').length)
  const recentlyUpdatedCount = computed(() => {
    if (!items.value.length) return 0
    const latestDate = items.value.reduce((latest, item) => {
      const value = item.updatedAt || item.createdAt
      return value > latest ? value : latest
    }, '')
    return items.value.filter((item) => (item.updatedAt || item.createdAt) === latestDate).length
  })

  return {
    activeCount,
    disabledCount,
    recentlyUpdatedCount
  }
}
