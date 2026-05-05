import { getAuthHeaders } from './apiAuth'

const API_BASE_URL = import.meta.env.VITE_REPORTS_API_BASE_URL || 'http://localhost:5215'

const parseResponse = async (response) => {
  const contentType = response.headers.get('content-type') || ''
  const body = contentType.includes('application/json') ? await response.json() : null
  if (!response.ok) throw new Error(body?.message || `Request failed with status ${response.status}`)
  return body
}

const toQueryString = (params = {}) => {
  const query = new URLSearchParams()
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') query.set(key, value)
  })
  const value = query.toString()
  return value ? `?${value}` : ''
}

export const getReport = async (reportType, params = {}) => {
  const response = await fetch(`${API_BASE_URL}/api/reports/${reportType}${toQueryString(params)}`, {
    headers: getAuthHeaders()
  })
  return parseResponse(response)
}
