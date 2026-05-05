import { createRouter, createWebHistory } from 'vue-router'
import { canViewModule, getAuthSession } from '../utils/authSession'

const routes = [
  { path: '/', redirect: '/login' },
  { path: '/login', component: () => import('../pages/Login.vue') },
  { path: '/dashboard', component: () => import('../pages/Dashboard.vue'), meta: { module: 'dashboard' } },
  { path: '/notifications', component: () => import('../pages/Notifications.vue'), meta: { module: 'dashboard' } },
  { path: '/users', component: () => import('../pages/UserManagement.vue'), meta: { module: 'users' } },
  { path: '/roles', component: () => import('../pages/RoleManagement.vue'), meta: { module: 'roles' } },
  { path: '/permissions', component: () => import('../pages/PermissionManagement.vue'), meta: { module: 'permissions' } },
  { path: '/user-code-setup', redirect: '/user-code-setup/departments' },
  { path: '/user-code-setup/departments', component: () => import('../pages/DepartmentCodeSetup.vue'), meta: { module: 'department-setup' } },
  { path: '/user-code-setup/locations', component: () => import('../pages/LocationCodeSetup.vue'), meta: { module: 'location-setup' } },
  { path: '/user-code-setup/location-types', component: () => import('../pages/LocationTypeSetup.vue'), meta: { module: 'location-type-setup' } },
  { path: '/user-code-setup/vehicle-types', component: () => import('../pages/VehicleTypeSetup.vue'), meta: { module: 'vehicle-type-setup' } },
  { path: '/user-code-setup/fuel-types', component: () => import('../pages/FuelTypeSetup.vue'), meta: { module: 'fuel-type-setup' } },
  { path: '/user-code-setup/trip-types', component: () => import('../pages/TripTypeSetup.vue'), meta: { module: 'trip-type-setup' } },
  { path: '/user-code-setup/cargo-types', component: () => import('../pages/CargoTypeSetup.vue'), meta: { module: 'cargo-type-setup' } },
  { path: '/user-code-setup/statuses', component: () => import('../pages/StatusSetup.vue'), meta: { module: 'status-setup' } },
  { path: '/user-code-setup/trip-priorities', component: () => import('../pages/TripPrioritySetup.vue'), meta: { module: 'trip-priority-setup' } },
  { path: '/user-code-setup/incident-types', component: () => import('../pages/IncidentTypeSetup.vue'), meta: { module: 'incident-type-setup' } },
  { path: '/user-code-setup/severities', component: () => import('../pages/SeveritySetup.vue'), meta: { module: 'severity-setup' } },
  { path: '/user-code-setup/expense-types', component: () => import('../pages/ExpenseTypeSetup.vue'), meta: { module: 'expense-type-setup' } },
  { path: '/user-code-setup/maintenance-types', component: () => import('../pages/MaintenanceTypeSetup.vue'), meta: { module: 'maintenance-type-setup' } },
  { path: '/user-code-setup/document-types', component: () => import('../pages/DocumentTypeSetup.vue'), meta: { module: 'document-type-setup' } },
  { path: '/vehicles', component: () => import('../pages/VehicleManagement.vue'), meta: { module: 'vehicles' } },
  { path: '/incidents', component: () => import('../pages/IncidentManagement.vue'), meta: { module: 'incidents' } },
  { path: '/trips', component: () => import('../pages/Trips.vue'), meta: { module: 'trips' } },
  { path: '/analytics', component: () => import('../pages/Placeholder.vue'), meta: { module: 'reports' } },
  { path: '/maintenance', component: () => import('../pages/MaintenanceTickets.vue'), meta: { module: 'maintenance-tickets' } },
  { path: '/maintenance/inventory', component: () => import('../pages/InventorySpareParts.vue'), meta: { module: 'inventory-parts' } },
  { path: '/reports', component: () => import('../pages/Placeholder.vue'), meta: { module: 'reports' } },
  { path: '/settings', component: () => import('../pages/Placeholder.vue'), meta: { module: 'settings' } }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to) => {
  const isLogin = to.path === '/login'
  const session = getAuthSession()
  if (isLogin && session) return '/dashboard'
  if (!isLogin && !session) return '/login'
  if (session && to.meta?.module && !canViewModule(to.meta.module)) return '/dashboard'
  return true
})

export default router
