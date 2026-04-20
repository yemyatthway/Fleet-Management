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

export const getUsers = () => request('/api/users')

export const createUser = (user) =>
  request('/api/users', {
    method: 'POST',
    body: JSON.stringify(user)
  })

export const updateUser = (userId, user) =>
  request(`/api/users/${userId}`, {
    method: 'PUT',
    body: JSON.stringify(user)
  })

export const updateUserStatus = (userId, status) =>
  request(`/api/users/${userId}/status`, {
    method: 'PATCH',
    body: JSON.stringify({ status })
  })

export const deleteUser = (userId) =>
  request(`/api/users/${userId}`, {
    method: 'DELETE'
  })
