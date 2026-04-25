import { roleCatalog } from './roles'
import { users as seededUsers } from './users'

const ACTIVE_STATUS = 'Active'
const clone = (value) => JSON.parse(JSON.stringify(value))
const normalizeText = (value) => String(value ?? '').trim().toLowerCase()
const timestamp = () => new Date().toISOString()

let usersStore = clone(seededUsers.value)
let rolesStore = [
  ...roleCatalog.map((role, index) => ({
    id: role.id,
    name: role.name,
    description: role.description,
    status: ACTIVE_STATUS,
    createdAt: `2024-01-${String(index + 10).padStart(2, '0')}T09:00:00.000Z`,
    updatedAt: `2026-03-${String(index + 2).padStart(2, '0')}T09:00:00.000Z`
  })),
  {
    id: 'compliance',
    name: 'Compliance',
    description: 'Audits fleet documents, permits, and safety records.',
    status: ACTIVE_STATUS,
    createdAt: '2024-04-18T09:00:00.000Z',
    updatedAt: '2026-03-18T09:00:00.000Z'
  }
]

let userCodeOptionsStore = [
  {
    id: 'dep-operations',
    type: 'Department',
    name: 'Operations',
    description: 'Dispatch planning and daily fleet coordination.',
    status: ACTIVE_STATUS,
    createdAt: '2024-01-10T09:00:00.000Z',
    updatedAt: '2026-03-01T09:00:00.000Z'
  },
  {
    id: 'dep-dispatch',
    type: 'Department',
    name: 'Dispatch',
    description: 'Route planning, trip oversight, and escalation handling.',
    status: ACTIVE_STATUS,
    createdAt: '2024-01-11T09:00:00.000Z',
    updatedAt: '2026-03-02T09:00:00.000Z'
  },
  {
    id: 'dep-fleet',
    type: 'Department',
    name: 'Fleet',
    description: 'Drivers, assignments, and utilization management.',
    status: ACTIVE_STATUS,
    createdAt: '2024-01-12T09:00:00.000Z',
    updatedAt: '2026-03-03T09:00:00.000Z'
  },
  {
    id: 'dep-maintenance',
    type: 'Department',
    name: 'Maintenance',
    description: 'Vehicle repairs, inspections, and parts workflows.',
    status: ACTIVE_STATUS,
    createdAt: '2024-01-13T09:00:00.000Z',
    updatedAt: '2026-03-04T09:00:00.000Z'
  },
  {
    id: 'loc-north-depot',
    type: 'Location',
    name: 'North Depot',
    description: 'Primary truck staging yard for regional routes.',
    status: ACTIVE_STATUS,
    createdAt: '2024-01-20T09:00:00.000Z',
    updatedAt: '2026-03-10T09:00:00.000Z'
  },
  {
    id: 'loc-central-hub',
    type: 'Location',
    name: 'Central Hub',
    description: 'Dispatch office and route command center.',
    status: ACTIVE_STATUS,
    createdAt: '2024-01-21T09:00:00.000Z',
    updatedAt: '2026-03-11T09:00:00.000Z'
  },
  {
    id: 'loc-east-depot',
    type: 'Location',
    name: 'East Depot',
    description: 'Last-mile distribution depot for metro deliveries.',
    status: ACTIVE_STATUS,
    createdAt: '2024-01-22T09:00:00.000Z',
    updatedAt: '2026-03-12T09:00:00.000Z'
  },
  {
    id: 'loc-south-depot',
    type: 'Location',
    name: 'South Depot',
    description: 'Night-shift yard and overflow vehicle parking.',
    status: ACTIVE_STATUS,
    createdAt: '2024-01-23T09:00:00.000Z',
    updatedAt: '2026-03-13T09:00:00.000Z'
  },
  {
    id: 'loc-service-bay-a',
    type: 'Location',
    name: 'Service Bay A',
    description: 'Heavy maintenance and inspection lane.',
    status: ACTIVE_STATUS,
    createdAt: '2024-01-24T09:00:00.000Z',
    updatedAt: '2026-03-14T09:00:00.000Z'
  },
  {
    id: 'loc-service-bay-b',
    type: 'Location',
    name: 'Service Bay B',
    description: 'Quick-turn maintenance and tire service lane.',
    status: ACTIVE_STATUS,
    createdAt: '2024-01-25T09:00:00.000Z',
    updatedAt: '2026-03-15T09:00:00.000Z'
  }
]

const compareValues = (left, right) => {
  if (typeof left === 'number' && typeof right === 'number') return left - right
  return String(left ?? '').localeCompare(String(right ?? ''), undefined, {
    numeric: true,
    sensitivity: 'base'
  })
}

const sortItems = (items, sortBy = 'id', sortOrder = 'asc') => {
  const direction = sortOrder === 'desc' ? -1 : 1
  return [...items].sort((left, right) => direction * compareValues(left[sortBy], right[sortBy]))
}

const paginateItems = (items, page = 1, pageSize = 10) => {
  const start = Math.max(page - 1, 0) * pageSize
  return items.slice(start, start + pageSize)
}

const nextNumericId = (items) =>
  String(
    items.reduce((max, item) => {
      const value = Number.parseInt(item.id, 10)
      return Number.isNaN(value) ? max : Math.max(max, value)
    }, 0) + 1
  )

const uniqueSlug = (items, baseName) => {
  const base = normalizeText(baseName).replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'item'
  let candidate = base
  let suffix = 2
  while (items.some((item) => item.id === candidate)) {
    candidate = `${base}-${suffix}`
    suffix += 1
  }
  return candidate
}

const ensureUniqueName = (items, name, type, currentId = null) => {
  const exists = items.some((item) => {
    const sameId = currentId && item.id === currentId
    const sameType = !type || item.type === type
    return !sameId && sameType && normalizeText(item.name) === normalizeText(name)
  })
  if (exists) throw new Error(`${name} already exists.`)
}

const getRoleRecord = (roleId) => {
  const role = rolesStore.find((item) => item.id === roleId)
  if (!role) throw new Error('Role not found.')
  return role
}

const getUserRecord = (userId) => {
  const user = usersStore.find((item) => item.id === userId)
  if (!user) throw new Error('User not found.')
  return user
}

const getCodeOptionRecord = (id) => {
  const item = userCodeOptionsStore.find((entry) => entry.id === id)
  if (!item) throw new Error('Record not found.')
  return item
}

const roleWithMembers = (role) => ({
  ...role,
  members: usersStore.filter((user) => user.role === role.name).length
})

const buildUsersResult = (params = {}) => {
  const search = normalizeText(params.search)
  const role = normalizeText(params.role)
  const filtered = usersStore.filter((user) => {
    const matchesRole = !role || normalizeText(user.role) === role
    const matchesQuery =
      !search ||
      [user.name, user.email, user.employeeId, user.department, user.location].some((field) =>
        normalizeText(field).includes(search)
      )
    return matchesRole && matchesQuery
  })

  const sorted = sortItems(filtered, params.sortBy || 'id', params.sortOrder || 'asc')
  return {
    items: clone(paginateItems(sorted, Number(params.page) || 1, Number(params.pageSize) || 10)),
    total: filtered.length,
    stats: {
      total: filtered.length,
      active: filtered.filter((user) => user.status === ACTIVE_STATUS).length,
      drivers: filtered.filter((user) => user.role === 'Driver').length,
      admins: filtered.filter((user) => user.role === 'Admin').length
    }
  }
}

const buildRolesResult = (params = {}) => {
  const search = normalizeText(params.search)
  const roleFilter = normalizeText(params.role)
  const filtered = rolesStore
    .map(roleWithMembers)
    .filter((role) => {
      const matchesRole = !roleFilter || normalizeText(role.name) === roleFilter
      const matchesQuery =
        !search ||
        [role.name, role.description, role.status].some((field) => normalizeText(field).includes(search))
      return matchesRole && matchesQuery
    })

  const sorted = sortItems(filtered, params.sortBy || 'id', params.sortOrder || 'asc')
  return {
    items: clone(paginateItems(sorted, Number(params.page) || 1, Number(params.pageSize) || 10)),
    total: filtered.length
  }
}

const buildCodeOptionsResult = (params = {}, fixedType = '') => {
  const search = normalizeText(params.search)
  const type = fixedType || params.type
  const filtered = userCodeOptionsStore.filter((item) => {
    const matchesType = !type || type === 'All' || item.type === type
    const matchesQuery =
      !search ||
      [item.name, item.description, item.status, item.type].some((field) =>
        normalizeText(field).includes(search)
      )
    return matchesType && matchesQuery
  })

  const sorted = sortItems(filtered, params.sortBy || 'id', params.sortOrder || 'asc')
  return {
    items: clone(paginateItems(sorted, Number(params.page) || 1, Number(params.pageSize) || 10)),
    total: filtered.length
  }
}

export const fakeBackend = {
  getUsers(params = {}) {
    return Promise.resolve(buildUsersResult(params))
  },

  createUser(payload) {
    const user = {
      ...payload,
      id: nextNumericId(usersStore),
      status: payload.status || ACTIVE_STATUS,
      joinDate: payload.joinDate || new Date().toISOString().slice(0, 10),
      lastLogin: payload.lastLogin || timestamp(),
      createdAt: timestamp(),
      updatedAt: timestamp()
    }
    usersStore = [...usersStore, user]
    return Promise.resolve(clone(user))
  },

  updateUser(userId, payload) {
    const current = getUserRecord(userId)
    const updated = {
      ...current,
      ...payload,
      id: current.id,
      updatedAt: timestamp()
    }
    usersStore = usersStore.map((user) => (user.id === userId ? updated : user))
    return Promise.resolve(clone(updated))
  },

  updateUserStatus(userId, status) {
    return this.updateUser(userId, { status })
  },

  deleteUser(userId) {
    getUserRecord(userId)
    usersStore = usersStore.filter((user) => user.id !== userId)
    return Promise.resolve()
  },

  getRoles(params = {}) {
    return Promise.resolve(buildRolesResult(params))
  },

  getRoleOptions() {
    return Promise.resolve(clone(rolesStore.map((role) => role.name)))
  },

  getRoleMembers(roleId) {
    const role = getRoleRecord(roleId)
    return Promise.resolve(clone(usersStore.filter((user) => user.role === role.name)))
  },

  createRole(payload) {
    ensureUniqueName(rolesStore, payload.name)
    const role = {
      id: uniqueSlug(rolesStore, payload.name),
      name: payload.name,
      description: payload.description,
      status: payload.status || ACTIVE_STATUS,
      createdAt: timestamp(),
      updatedAt: timestamp()
    }
    rolesStore = [...rolesStore, role]
    return Promise.resolve(roleWithMembers(role))
  },

  updateRole(roleId, payload) {
    const current = getRoleRecord(roleId)
    ensureUniqueName(rolesStore, payload.name, null, roleId)

    if (current.name !== payload.name) {
      usersStore = usersStore.map((user) =>
        user.role === current.name ? { ...user, role: payload.name, updatedAt: timestamp() } : user
      )
    }

    const updated = {
      ...current,
      ...payload,
      id: current.id,
      updatedAt: timestamp()
    }
    rolesStore = rolesStore.map((role) => (role.id === roleId ? updated : role))
    return Promise.resolve(roleWithMembers(updated))
  },

  deleteRole(roleId) {
    const role = roleWithMembers(getRoleRecord(roleId))
    if (role.members > 0) {
      return Promise.reject(new Error(`Cannot delete ${role.name} while users are assigned to it.`))
    }
    rolesStore = rolesStore.filter((item) => item.id !== roleId)
    return Promise.resolve()
  },

  getUserCodeOptions(params = {}) {
    return Promise.resolve(buildCodeOptionsResult(params))
  },

  getDepartmentCodeOptions(params = {}) {
    return Promise.resolve(buildCodeOptionsResult(params, 'Department'))
  },

  getLocationCodeOptions(params = {}) {
    return Promise.resolve(buildCodeOptionsResult(params, 'Location'))
  },

  getDepartmentOptions() {
    return Promise.resolve(
      clone(
        userCodeOptionsStore
          .filter((item) => item.type === 'Department' && item.status === ACTIVE_STATUS)
          .map((item) => item.name)
      )
    )
  },

  getLocationOptions() {
    return Promise.resolve(
      clone(
        userCodeOptionsStore
          .filter((item) => item.type === 'Location' && item.status === ACTIVE_STATUS)
          .map((item) => item.name)
      )
    )
  },

  createUserCodeOption(payload) {
    ensureUniqueName(userCodeOptionsStore, payload.name, payload.type)
    const item = {
      id: uniqueSlug(userCodeOptionsStore, `${payload.type}-${payload.name}`),
      type: payload.type,
      name: payload.name,
      description: payload.description || null,
      status: payload.status || ACTIVE_STATUS,
      createdAt: timestamp(),
      updatedAt: timestamp()
    }
    userCodeOptionsStore = [...userCodeOptionsStore, item]
    return Promise.resolve(clone(item))
  },

  updateUserCodeOption(id, payload) {
    const current = getCodeOptionRecord(id)
    ensureUniqueName(userCodeOptionsStore, payload.name, payload.type, id)

    const updated = {
      ...current,
      ...payload,
      id: current.id,
      updatedAt: timestamp()
    }
    userCodeOptionsStore = userCodeOptionsStore.map((item) => (item.id === id ? updated : item))
    return Promise.resolve(clone(updated))
  },

  deleteUserCodeOption(id) {
    getCodeOptionRecord(id)
    userCodeOptionsStore = userCodeOptionsStore.filter((item) => item.id !== id)
    return Promise.resolve()
  }
}
