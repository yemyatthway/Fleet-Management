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

const isAdminSession = () => {
  const session = getAuthSession()
  const role = String(session?.user?.role || session?.user?.roleId || '').toLowerCase()
  return role === 'admin'
}

export const canViewModule = (moduleKey) => {
  if (!moduleKey) return true
  const permission = getPermission(moduleKey)
  if (!permission && isAdminSession()) return true
  return Boolean(permission?.canView)
}

export const canCreateModule = (moduleKey) => {
  const permission = getPermission(moduleKey)
  if (!permission && isAdminSession()) return true
  return Boolean(permission?.canCreate)
}

export const canEditModule = (moduleKey) => {
  const permission = getPermission(moduleKey)
  if (!permission && isAdminSession()) return true
  return Boolean(permission?.canEdit)
}

export const canDeleteModule = (moduleKey) => {
  const permission = getPermission(moduleKey)
  if (!permission && isAdminSession()) return true
  return Boolean(permission?.canDelete)
}
