import { getCurrentUser } from '../utils/authSession'

export const getAuthHeaders = () => {
  const user = getCurrentUser()
  const headers = {}
  if (user?.roleId) headers['X-Fleet-Role-Id'] = user.roleId
  if (user?.id) headers['X-Fleet-User-Id'] = user.id
  if (user?.name) headers['X-Fleet-User-Name'] = user.name
  return headers
}
