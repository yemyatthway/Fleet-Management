export function normalizeSortOption(sortBy) {
  const firstSort = sortBy?.[0]
  if (!firstSort) return null
  if (typeof firstSort === 'string') return { key: firstSort, order: 'asc' }

  const key = firstSort.key || firstSort.field || ''
  const order =
    firstSort.order ||
    (typeof firstSort.desc === 'boolean' ? (firstSort.desc ? 'desc' : 'asc') : 'asc')

  return key ? { key, order } : null
}
