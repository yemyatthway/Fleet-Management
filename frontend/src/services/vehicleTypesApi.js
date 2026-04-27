const API_BASE_URL = import.meta.env.VITE_VEHICLE_TYPES_API_BASE_URL || 'http://localhost:5215'

const parseResponse = async (response) => {
  if (response.status === 204) return null

  const contentType = response.headers.get('content-type') || ''
  const body = contentType.includes('application/json') ? await response.json() : null

  if (!response.ok) {
    throw new Error(body?.message || `Request failed with status ${response.status}`)
  }

  return body
}

const request = async (path, options = {}) => {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...options.headers
    },
    ...options
  })

  return parseResponse(response)
}

const toQueryString = (params = {}) => {
  const query = new URLSearchParams()
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') query.set(key, value)
  })
  const value = query.toString()
  return value ? `?${value}` : ''
}

export const getVehicleTypeCodeOptions = (params = {}) =>
  request(`/api/vehicle-types${toQueryString(params)}`)

export const getVehicleTypeOptions = () => request('/api/vehicle-types/options')

export const createVehicleTypeCodeOption = (payload) =>
  request('/api/vehicle-types', {
    method: 'POST',
    body: JSON.stringify(payload)
  })

export const updateVehicleTypeCodeOption = (id, payload) =>
  request(`/api/vehicle-types/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })

export const deleteVehicleTypeCodeOption = (id) =>
  request(`/api/vehicle-types/${id}`, {
    method: 'DELETE'
  })
