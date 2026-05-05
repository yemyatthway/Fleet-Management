import { getAuthHeaders } from './apiAuth'

const API_BASE_URL = import.meta.env.VITE_EXPENSES_API_BASE_URL || 'http://localhost:5215'

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

export const getExpenses = (params = {}) => request(`/api/expenses${toQueryString(params)}`)
export const createExpense = (payload) => request('/api/expenses', { method: 'POST', body: JSON.stringify(payload) })
export const updateExpense = (id, payload) => request(`/api/expenses/${id}`, { method: 'PUT', body: JSON.stringify(payload) })
export const deleteExpense = (id) => request(`/api/expenses/${id}`, { method: 'DELETE' })
