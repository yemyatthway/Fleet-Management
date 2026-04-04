export const roleCatalog = [
  {
    id: 'admin',
    name: 'Admin',
    description: 'Full platform access with governance controls.',
    badgeClass: 'role-admin'
  },
  {
    id: 'dispatcher',
    name: 'Dispatcher',
    description: 'Schedules routes, assigns drivers, and monitors trips.',
    badgeClass: 'role-dispatcher'
  },
  {
    id: 'driver',
    name: 'Driver',
    description: 'Executes assigned routes and updates trip status.',
    badgeClass: 'role-driver'
  },
  {
    id: 'mechanic',
    name: 'Mechanic',
    description: 'Manages inspections, repairs, and maintenance logs.',
    badgeClass: 'role-mechanic'
  }
]

export const roleNames = roleCatalog.map((role) => role.name)

export const roleClassMap = roleCatalog.reduce((acc, role) => {
  acc[role.name] = role.badgeClass
  return acc
}, {})
