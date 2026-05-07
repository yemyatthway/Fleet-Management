const SESSION_KEY = 'fleet.auth.session'
const REMEMBERED_LOGIN_KEY = 'fleet.auth.rememberedLogin'

export const getAuthSession = () => {
  try {
    const session = JSON.parse(localStorage.getItem(SESSION_KEY) || sessionStorage.getItem(SESSION_KEY) || 'null')
    if (session?.expiresAt && new Date(session.expiresAt).getTime() <= Date.now()) {
      clearAuthSession()
      return null
    }
    return session
  } catch {
    return null
  }
}

export const setAuthSession = (session, remember = true) => {
  const storage = remember ? localStorage : sessionStorage
  const otherStorage = remember ? sessionStorage : localStorage
  storage.setItem(SESSION_KEY, JSON.stringify({ ...session, remember }))
  otherStorage.removeItem(SESSION_KEY)
}

export const clearAuthSession = () => {
  localStorage.removeItem(SESSION_KEY)
  sessionStorage.removeItem(SESSION_KEY)
}

export const getCurrentUser = () => getAuthSession()?.user || null
export const getAuthToken = () => getAuthSession()?.token || ''

export const getRememberedLogin = () => {
  try {
    return JSON.parse(localStorage.getItem(REMEMBERED_LOGIN_KEY) || 'null')
  } catch {
    return null
  }
}

export const setRememberedLogin = (email) => {
  const normalizedEmail = String(email || '').trim()
  if (!normalizedEmail) return
  localStorage.setItem(REMEMBERED_LOGIN_KEY, JSON.stringify({ email: normalizedEmail }))
}

export const clearRememberedLogin = () => {
  localStorage.removeItem(REMEMBERED_LOGIN_KEY)
}

export const getPermission = (moduleKey) =>
  getAuthSession()?.permissions?.find((permission) => permission.moduleKey === moduleKey) || null

const isAdminSession = () => {
  const session = getAuthSession()
  const role = String(session?.user?.role || session?.user?.roleId || '').toLowerCase()
  return role === 'admin'
}

export const canViewModule = (moduleKey) => {
  if (!moduleKey) return true
  if (isDispatcherSession() && String(moduleKey).endsWith('-setup')) return false
  const permission = getPermission(moduleKey)
  if (!permission && isAdminSession()) return true
  return Boolean(permission?.canView)
}

export const canCreateModule = (moduleKey) => {
  if (isDispatcherSession() && String(moduleKey).endsWith('-setup')) return false
  const permission = getPermission(moduleKey)
  if (!permission && isAdminSession()) return true
  return Boolean(permission?.canCreate)
}

export const canEditModule = (moduleKey) => {
  if (isDispatcherSession() && String(moduleKey).endsWith('-setup')) return false
  const permission = getPermission(moduleKey)
  if (!permission && isAdminSession()) return true
  return Boolean(permission?.canEdit)
}

export const canDeleteModule = (moduleKey) => {
  if (isDispatcherSession() && String(moduleKey).endsWith('-setup')) return false
  const permission = getPermission(moduleKey)
  if (!permission && isAdminSession()) return true
  return Boolean(permission?.canDelete)
}

const isDispatcherSession = () => {
  const session = getAuthSession()
  const role = String(session?.user?.roleId || session?.user?.role || '').toLowerCase()
  return role === 'dispatcher'
}
