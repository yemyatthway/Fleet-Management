import { createApiClient, DEFAULT_API_BASE_URL } from './httpClient'

const { request } = createApiClient(import.meta.env.VITE_INVENTORY_PARTS_API_BASE_URL || DEFAULT_API_BASE_URL)

const appendIfPresent = (formData, key, value) => {
  if (value === undefined || value === null || value === '') return
  formData.append(key, value)
}

const buildPartFormData = (part) => {
  const formData = new FormData()
  appendIfPresent(formData, 'name', part.name)
  appendIfPresent(formData, 'partNo', part.partNo)
  appendIfPresent(formData, 'category', part.category)
  appendIfPresent(formData, 'stock', part.stock)
  appendIfPresent(formData, 'reorderPoint', part.reorderPoint)
  appendIfPresent(formData, 'supplier', part.supplier)
  appendIfPresent(formData, 'unitCost', part.unitCost)
  appendIfPresent(formData, 'location', part.location)
  if (part.removeImage) formData.append('removeImage', 'true')
  if (part.imageFile instanceof File) formData.append('imageFile', part.imageFile)
  return formData
}

export const getInventoryParts = () => request('/api/inventory-parts')
export const createInventoryPart = (payload) =>
  request('/api/inventory-parts', { method: 'POST', body: buildPartFormData(payload) })
export const updateInventoryPart = (id, payload) =>
  request(`/api/inventory-parts/${id}`, { method: 'PUT', body: buildPartFormData(payload) })
export const deleteInventoryPart = (id) =>
  request(`/api/inventory-parts/${id}`, { method: 'DELETE' })
