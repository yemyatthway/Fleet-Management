const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5215'

const parseResponse = async (response) => {
  const contentType = response.headers.get('content-type') || ''
  const body = contentType.includes('application/json') ? await response.json() : null

  if (!response.ok) {
    const error = new Error(body?.message || `Request failed with status ${response.status}`)
    console.error('[rolesApi] request failed', { status: response.status, body })
    throw error
  }

  return body
}

const request = async (path, options = {}) => {
  try {
    const response = await fetch(`${API_BASE_URL}${path}`, {
      headers: {
        'Content-Type': 'application/json',
        ...options.headers
      },
      ...options
    })

    return parseResponse(response)
  } catch (error) {
    console.error('[rolesApi] request error', { path, error })
    throw error
  }
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
