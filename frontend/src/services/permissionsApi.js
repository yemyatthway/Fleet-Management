const API_BASE_URL = import.meta.env.VITE_ROLES_API_BASE_URL || 'http://localhost:5215'

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

export const getPermissions = () => request('/api/permissions')

export const updatePermissions = (permissions) =>
  request('/api/permissions', {
    method: 'PUT',
    body: JSON.stringify({ permissions })
  })
