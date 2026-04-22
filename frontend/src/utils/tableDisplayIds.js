const DEFAULT_PAD_LENGTH = 3

const padDisplayNumber = (value, padLength = DEFAULT_PAD_LENGTH) =>
  String(value).padStart(padLength, '0')

export const buildDisplayId = (prefix, index, padLength = DEFAULT_PAD_LENGTH) =>
  `${prefix}-${padDisplayNumber(index, padLength)}`

export const attachDisplayIds = (items, page, itemsPerPage, resolvePrefix) => {
  const safePage = Math.max(Number(page) || 1, 1)
  const safeItemsPerPage = Math.max(Number(itemsPerPage) || 1, 1)
  const startIndex = (safePage - 1) * safeItemsPerPage

  return items.map((item, index) => ({
    ...item,
    displayId: buildDisplayId(resolvePrefix(item), startIndex + index + 1)
  }))
}
