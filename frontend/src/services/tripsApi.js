const API_BASE_URL = import.meta.env.VITE_TRIPS_API_BASE_URL || 'http://localhost:5215'
import { getAuthHeaders } from './apiAuth'

const parseResponse = async (response) => {
  if (response.status === 204) return null
  const contentType = response.headers.get('content-type') || ''
  const body = contentType.includes('application/json') ? await response.json() : null
  if (!response.ok) throw new Error(body?.message || `Request failed with status ${response.status}`)
  return body
}

const request = async (path, options = {}) => {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...getAuthHeaders(),
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

export const getTrips = (params = {}) => request(`/api/trips${toQueryString(params)}`)

export const createTrip = (payload) =>
  request('/api/trips', {
    method: 'POST',
    body: JSON.stringify(payload)
  })

export const updateTrip = (id, payload) =>
  request(`/api/trips/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })

export const deleteTrip = (id) =>
  request(`/api/trips/${id}`, {
    method: 'DELETE'
  })
