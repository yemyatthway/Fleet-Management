const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5215'

const parseResponse = async (response) => {
  const contentType = response.headers.get('content-type') || ''
  const body = contentType.includes('application/json') ? await response.json() : null

  if (!response.ok) {
    const error = new Error(body?.message || `Request failed with status ${response.status}`)
    console.error('[userCodeOptionsApi] request failed', { status: response.status, body })
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
    console.error('[userCodeOptionsApi] request error', { path, error })
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

export const getUserCodeOptions = (params = {}) => request(`/api/user-code-options${toQueryString(params)}`)

export const getDepartmentOptions = () => request('/api/user-code-options/options?type=Department')

export const getLocationOptions = () => request('/api/user-code-options/options?type=Location')

export const createUserCodeOption = (payload) =>
  request('/api/user-code-options', {
    method: 'POST',
    body: JSON.stringify(payload)
  })

export const updateUserCodeOption = (id, payload) =>
  request(`/api/user-code-options/${id}`, {
    method: 'PUT',
    body: JSON.stringify(payload)
  })

export const deleteUserCodeOption = (id) =>
  request(`/api/user-code-options/${id}`, {
    method: 'DELETE'
  })
