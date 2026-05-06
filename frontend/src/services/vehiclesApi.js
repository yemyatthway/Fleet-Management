import { createApiClient, DEFAULT_API_BASE_URL, resolveAssetUrl, toQueryString } from './httpClient'

const { request, baseUrl } = createApiClient(import.meta.env.VITE_VEHICLES_API_BASE_URL || DEFAULT_API_BASE_URL)

const normalizeVehicle = (vehicle) => ({
  ...vehicle,
  image: resolveAssetUrl(vehicle?.image, baseUrl),
  driverImage: resolveAssetUrl(vehicle?.driverImage, baseUrl)
})

const appendIfPresent = (formData, key, value) => {
  if (value === undefined || value === null || value === '') return
  formData.append(key, value)
}

const buildVehicleFormData = (vehicle) => {
  const formData = new FormData()
  appendIfPresent(formData, 'plate', vehicle.plate)
  appendIfPresent(formData, 'region', vehicle.region)
  appendIfPresent(formData, 'type', vehicle.type)
  appendIfPresent(formData, 'model', vehicle.model)
  appendIfPresent(formData, 'make', vehicle.make)
  appendIfPresent(formData, 'year', vehicle.year)
  appendIfPresent(formData, 'color', vehicle.color)
  appendIfPresent(formData, 'status', vehicle.status)
  appendIfPresent(formData, 'ownership', vehicle.ownership)
  appendIfPresent(formData, 'driver', vehicle.driver)
  appendIfPresent(formData, 'depot', vehicle.depot)
  appendIfPresent(formData, 'capacity', vehicle.capacity)
  appendIfPresent(formData, 'fuelCapacity', vehicle.fuelCapacity)
  appendIfPresent(formData, 'fuelType', vehicle.fuelType)
  appendIfPresent(formData, 'vin', vehicle.vin)
  appendIfPresent(formData, 'engineNo', vehicle.engineNo)
  appendIfPresent(formData, 'odometer', vehicle.odometer)
  appendIfPresent(formData, 'lastService', vehicle.lastService)
  appendIfPresent(formData, 'nextService', vehicle.nextService)
  appendIfPresent(formData, 'serviceNote', vehicle.serviceNote)
  appendIfPresent(formData, 'purchaseCost', vehicle.purchaseCost)
  appendIfPresent(formData, 'registrationNo', vehicle.registrationNo)
  appendIfPresent(formData, 'registrationExpiry', vehicle.registrationExpiry)
  appendIfPresent(formData, 'roadTaxExpiry', vehicle.roadTaxExpiry)
  appendIfPresent(formData, 'insuranceExpiry', vehicle.insuranceExpiry)
  appendIfPresent(formData, 'insuranceProvider', vehicle.insuranceProvider)
  appendIfPresent(formData, 'insurancePolicy', vehicle.insurancePolicy)
  appendIfPresent(formData, 'inspectionDue', vehicle.inspectionDue)
  appendIfPresent(formData, 'acquiredDate', vehicle.acquiredDate)
  if (vehicle.removeVehicleImage) formData.append('removeVehicleImage', 'true')
  if (vehicle.removeDriverImage) formData.append('removeDriverImage', 'true')

  if (vehicle.vehicleImageFile instanceof File) formData.append('vehicleImageFile', vehicle.vehicleImageFile)
  if (vehicle.driverImageFile instanceof File) formData.append('driverImageFile', vehicle.driverImageFile)

  return formData
}

export const getVehicles = async (params = {}) => {
  const vehicles = await request(`/api/vehicles${toQueryString(params)}`)
  return Array.isArray(vehicles) ? vehicles.map(normalizeVehicle) : []
}

export const createVehicle = (payload) =>
  request('/api/vehicles', {
    method: 'POST',
    body: buildVehicleFormData(payload)
  }).then(normalizeVehicle)

export const updateVehicle = (id, payload) =>
  request(`/api/vehicles/${id}`, {
    method: 'PUT',
    body: buildVehicleFormData(payload)
  }).then(normalizeVehicle)

export const updateVehicleStatus = (id, status) =>
  request(`/api/vehicles/${id}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status })
  }).then(normalizeVehicle)

export const deleteVehicle = (id) =>
  request(`/api/vehicles/${id}`, {
    method: 'DELETE'
  })
