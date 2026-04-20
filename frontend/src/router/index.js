import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  { path: '/', redirect: '/login' },
  { path: '/login', component: () => import('../pages/Login.vue') },
  { path: '/dashboard', component: () => import('../pages/Dashboard.vue') },
  { path: '/notifications', component: () => import('../pages/Notifications.vue') },
  { path: '/users', component: () => import('../pages/UserManagement.vue') },
  { path: '/roles', component: () => import('../pages/RoleManagement.vue') },
  { path: '/vehicles', component: () => import('../pages/VehicleManagement.vue') },
  { path: '/incidents', component: () => import('../pages/IncidentManagement.vue') },
  { path: '/trips', component: () => import('../pages/Placeholder.vue') },
  { path: '/analytics', component: () => import('../pages/Placeholder.vue') },
  { path: '/maintenance', component: () => import('../pages/MaintenanceTickets.vue') },
  { path: '/maintenance/inventory', component: () => import('../pages/InventorySpareParts.vue') },
  { path: '/reports', component: () => import('../pages/Placeholder.vue') },
  { path: '/settings', component: () => import('../pages/Placeholder.vue') }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
