const DEFAULT_PAD_LENGTH = 3

const padDisplayNumber = (value, padLength = DEFAULT_PAD_LENGTH) =>
  String(value).padStart(padLength, '0')

export const buildDisplayId = (prefix, index, padLength = DEFAULT_PAD_LENGTH) =>
  `${prefix}-${padDisplayNumber(index, padLength)}`

export const attachDisplayIds = (
  items,
  page,
  itemsPerPage,
  useRecordOrder,
  resolvePrefix,
  options = {}
) => {
  const safePage = Math.max(Number(page) || 1, 1)
  const safeItemsPerPage = Math.max(Number(itemsPerPage) || 1, 1)
  const startIndex = (safePage - 1) * safeItemsPerPage
  const shouldUseRecordOrder = useRecordOrder !== false
  const safeTotal = Math.max(Number(options.total) || 0, 0)
  const normalizedSortBy = String(options.sortBy || '').toLowerCase()
  const normalizedSortOrder = String(options.sortOrder || '').toLowerCase()
  const isSetupIdDescSort = !shouldUseRecordOrder && normalizedSortBy === 'id' && normalizedSortOrder === 'desc'
  const recordOrderById = shouldUseRecordOrder
    ? new Map(
        [...items]
          .sort((a, b) => (Number(a.id) || 0) - (Number(b.id) || 0))
          .map((item, index) => [item.id, startIndex + index + 1])
      )
    : new Map()

  return items.map((item, index) => ({
    ...item,
    displayId: buildDisplayId(
      resolvePrefix(item),
      shouldUseRecordOrder && Number(item.displayOrder) > 0
        ? Number(item.displayOrder)
        : shouldUseRecordOrder && recordOrderById.has(item.id)
          ? Number(recordOrderById.get(item.id))
          : isSetupIdDescSort && safeTotal > 0
            ? Math.max(safeTotal - startIndex - index, 1)
            : startIndex + index + 1
    )
  }))
}
