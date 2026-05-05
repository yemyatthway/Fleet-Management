const API_BASE_URL = import.meta.env.VITE_ROLES_API_BASE_URL || 'http://localhost:5215'
import { getAuthHeaders } from './apiAuth'

const resolveAssetUrl = (value) => {
  if (!value) return ''
  if (/^https?:\/\//i.test(value) || value.startsWith('data:')) return value
  if (/^file:\/\/\/uploads\//i.test(value)) {
    const relativePath = value.replace(/^file:\/\//i, '')
    return `${API_BASE_URL}${relativePath.startsWith('/') ? relativePath : `/${relativePath}`}`
  }
  if (/^uploads\//i.test(value)) return `${API_BASE_URL}/${value}`
  if (value.startsWith('/')) return `${API_BASE_URL}${value}`
  return value
}

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

export const getRoles = (params = {}) => request(`/api/roles${toQueryString(params)}`)

export const getRoleOptions = () => request('/api/roles/options')

export const getRoleMembers = async (roleId) => {
  const result = await request(`/api/roles/${roleId}/members`)
  return Array.isArray(result)
    ? result.map((member) => ({
        ...member,
        avatar: resolveAssetUrl(member?.avatar)
      }))
    : []
}

export const createRole = (role) =>
  request('/api/roles', {
    method: 'POST',
    body: JSON.stringify(role)
  })

export const updateRole = (roleId, role) =>
  request(`/api/roles/${roleId}`, {
    method: 'PUT',
    body: JSON.stringify(role)
  })

export const deleteRole = (roleId) =>
  request(`/api/roles/${roleId}`, {
    method: 'DELETE'
  })
