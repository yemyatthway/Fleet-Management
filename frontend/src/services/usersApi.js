const API_BASE_URL = import.meta.env.VITE_USERS_API_BASE_URL || 'http://localhost:5215'
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
  const isFormData = options.body instanceof FormData
  const response = await fetch(`${API_BASE_URL}${path}`, {
    headers: isFormData
      ? { ...getAuthHeaders(), ...options.headers }
      : {
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

const normalizeUser = (user) => ({
  ...user,
  avatar: resolveAssetUrl(user?.avatar),
  nrcFront: resolveAssetUrl(user?.nrcFront),
  nrcBack: resolveAssetUrl(user?.nrcBack)
})

export const getUsers = async (params = {}) => {
  const result = await request(`/api/users${toQueryString(params)}`)
  return {
    ...result,
    items: Array.isArray(result?.items) ? result.items.map(normalizeUser) : []
  }
}

export const getUserOptions = (params = {}) =>
  request(`/api/users/options${toQueryString(params)}`)

const appendIfPresent = (formData, key, value) => {
  if (value === undefined || value === null || value === '') return
  formData.append(key, value)
}

const buildUserFormData = (user) => {
  const formData = new FormData()
  appendIfPresent(formData, 'name', user.name)
  appendIfPresent(formData, 'nrcNumber', user.nrcNumber)
  appendIfPresent(formData, 'email', user.email)
  appendIfPresent(formData, 'role', user.role)
  appendIfPresent(formData, 'status', user.status)
  appendIfPresent(formData, 'phone', user.phone)
  appendIfPresent(formData, 'department', user.department)
  appendIfPresent(formData, 'title', user.title)
  appendIfPresent(formData, 'location', user.location)
  appendIfPresent(formData, 'manager', user.manager)
  appendIfPresent(formData, 'licenseNumber', user.licenseNumber)
  appendIfPresent(formData, 'licenseClass', user.licenseClass)
  appendIfPresent(formData, 'licenseExpiry', user.licenseExpiry)
  appendIfPresent(formData, 'emergencyContactName', user.emergencyContactName)
  appendIfPresent(formData, 'emergencyContactRelation', user.emergencyContactRelation)
  appendIfPresent(formData, 'emergencyContactPhone', user.emergencyContactPhone)
  appendIfPresent(formData, 'address', user.address)
  formData.append('twoFactorEnabled', user.twoFactorEnabled ? 'true' : 'false')
  appendIfPresent(formData, 'notes', user.notes)

  if (user.avatarFile instanceof File) formData.append('avatarFile', user.avatarFile)
  if (user.nrcFrontFile instanceof File) formData.append('nrcFrontFile', user.nrcFrontFile)
  if (user.nrcBackFile instanceof File) formData.append('nrcBackFile', user.nrcBackFile)

  return formData
}

export const createUser = (user) =>
  request('/api/users', {
    method: 'POST',
    body: buildUserFormData(user)
  }).then(normalizeUser)

export const updateUser = (userId, user) =>
  request(`/api/users/${userId}`, {
    method: 'PUT',
    body: buildUserFormData(user)
  }).then(normalizeUser)

export const updateUserStatus = (userId, status) =>
  request(`/api/users/${userId}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status })
  }).then(normalizeUser)

export const deleteUser = (userId) =>
  request(`/api/users/${userId}`, {
    method: 'DELETE'
  })
