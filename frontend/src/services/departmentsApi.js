const API_BASE_URL = import.meta.env.VITE_DEPARTMENTS_API_BASE_URL || 'http://localhost:5215'

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

export const getDepartmentCodeOptions = (params = {}) => request(`/api/departments${toQueryString(params)}`)

export const getDepartmentOptions = () => request('/api/departments/options')

export const createDepartmentCodeOption = (payload) =>
  request('/api/departments', {
    method: 'POST',
    body: JSON.stringify(payload)
  })

export const updateDepartmentCodeOption = (id, payload) =>
  request(`/api/departments/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })

export const deleteDepartmentCodeOption = (id) =>
  request(`/api/departments/${id}`, {
    method: 'DELETE'
  })
