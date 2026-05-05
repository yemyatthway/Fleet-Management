import { getCurrentUser } from '../utils/authSession'

export const getAuthHeaders = () => {
  const roleId = getCurrentUser()?.roleId
  return roleId ? { 'X-Fleet-Role-Id': roleId } : {}
}
