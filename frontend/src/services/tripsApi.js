import { createApiClient, DEFAULT_API_BASE_URL, toQueryString } from './httpClient'

const { request } = createApiClient(import.meta.env.VITE_TRIPS_API_BASE_URL || DEFAULT_API_BASE_URL)

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
