import { createApiClient, DEFAULT_API_BASE_URL, resolveAssetUrl, toQueryString } from './httpClient'

const { request, baseUrl } = createApiClient(import.meta.env.VITE_USERS_API_BASE_URL || DEFAULT_API_BASE_URL)

const normalizeUser = (user) => ({
  ...user,
  avatar: resolveAssetUrl(user?.avatar, baseUrl),
  nrcFront: resolveAssetUrl(user?.nrcFront, baseUrl),
  nrcBack: resolveAssetUrl(user?.nrcBack, baseUrl)
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
