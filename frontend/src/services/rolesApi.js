const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5215'

const parseResponse = async (response) => {
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

export const getRoles = () => request('/api/roles')

export const getRoleMembers = (roleId) => request(`/api/roles/${roleId}/members`)

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
