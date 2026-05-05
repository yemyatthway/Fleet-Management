const SESSION_KEY = 'fleet.auth.session'

export const getAuthSession = () => {
  try {
    return JSON.parse(localStorage.getItem(SESSION_KEY) || 'null')
  } catch {
    return null
  }
}

export const setAuthSession = (session) => {
  localStorage.setItem(SESSION_KEY, JSON.stringify(session))
}

export const clearAuthSession = () => {
  localStorage.removeItem(SESSION_KEY)
}

export const getCurrentUser = () => getAuthSession()?.user || null

export const getPermission = (moduleKey) =>
  getAuthSession()?.permissions?.find((permission) => permission.moduleKey === moduleKey) || null

export const canViewModule = (moduleKey) => {
  if (!moduleKey) return true
  return Boolean(getPermission(moduleKey)?.canView)
}

export const canCreateModule = (moduleKey) => Boolean(getPermission(moduleKey)?.canCreate)

export const canEditModule = (moduleKey) => Boolean(getPermission(moduleKey)?.canEdit)

export const canDeleteModule = (moduleKey) => Boolean(getPermission(moduleKey)?.canDelete)
