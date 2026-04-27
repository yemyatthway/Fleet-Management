const API_BASE_URL = import.meta.env.VITE_FUEL_TYPES_API_BASE_URL || 'http://localhost:5215'

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

export const getFuelTypeCodeOptions = (params = {}) =>
  request(`/api/fuel-types${toQueryString(params)}`)

export const getFuelTypeOptions = () => request('/api/fuel-types/options')

export const createFuelTypeCodeOption = (payload) =>
  request('/api/fuel-types', {
    method: 'POST',
    body: JSON.stringify(payload)
  })

export const updateFuelTypeCodeOption = (id, payload) =>
  request(`/api/fuel-types/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })

export const deleteFuelTypeCodeOption = (id) =>
  request(`/api/fuel-types/${id}`, {
    method: 'DELETE'
  })
