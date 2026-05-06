const roleClassMap = {
  Admin: 'role-admin',
  Dispatcher: 'role-dispatcher',
  Driver: 'role-driver',
  Mechanic: 'role-mechanic'
}

export const roleClassFor = (role) => roleClassMap[role] || 'role-driver'
