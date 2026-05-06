<template>
  <div ref="sidebarRef" class="sidebar-wrap">
    <div class="logo-row">
      <div class="logo-icon">
        <v-icon icon="mdi-truck" size="24" />
      </div>
      <div>
        <h1 class="logo-title">FleetManager</h1>
      </div>
    </div>

    <nav class="nav-list">
      <button
        v-for="item in visibleMenuItems"
        :key="item.label"
        type="button"
        class="nav-item"
        :class="{ active: route.path === item.path }"
        @click="navigateTo(item.path)"
      >
        <v-icon :icon="item.icon" size="20" />
        <span>{{ item.label }}</span>
      </button>

      <div v-if="visibleMaintenanceItems.length" class="nav-group">
        <button
          type="button"
          class="nav-item nav-group-toggle"
          :class="{ active: isMaintenanceRoute(route.path) }"
          @click="maintenanceOpen = !maintenanceOpen"
        >
          <v-icon icon="mdi-alert-circle-outline" size="20" />
          <span>Maintenance</span>
          <v-icon class="chevron" :class="{ open: maintenanceOpen }" icon="mdi-chevron-down" size="18" />
        </button>
        <div v-show="maintenanceOpen" class="nav-sublist">
          <button
            v-for="item in visibleMaintenanceItems"
            :key="item.label"
            type="button"
            class="nav-subitem"
            :class="{ active: route.path === item.path }"
            @click="navigateTo(item.path)"
          >
            <v-icon :icon="item.icon" size="18" />
            <span>{{ item.label }}</span>
          </button>
        </div>
      </div>

      <div v-if="visibleUserItems.length" class="nav-group">
        <button
          type="button"
          class="nav-item nav-group-toggle"
          :class="{ active: isUserManagementRoute(route.path) }"
          @click="userOpen = !userOpen"
        >
          <v-icon icon="mdi-account-group" size="20" />
          <span>User Management</span>
          <v-icon class="chevron" :class="{ open: userOpen }" icon="mdi-chevron-down" size="18" />
        </button>
        <div v-show="userOpen" class="nav-sublist">
          <button
            v-for="item in visibleUserItems"
            :key="item.label"
            type="button"
            class="nav-subitem"
            :class="{ active: route.path === item.path }"
            @click="navigateTo(item.path)"
          >
            <v-icon :icon="item.icon" size="18" />
            <span>{{ item.label }}</span>
          </button>
        </div>
      </div>

      <div v-if="visibleSetupItems.length" class="nav-group">
        <button
          type="button"
          class="nav-item nav-group-toggle"
          :class="{ active: route.path.startsWith('/user-code-setup') }"
          @click="setupOpen = !setupOpen"
        >
          <v-icon icon="mdi-cog-outline" size="20" />
          <span>Setup</span>
          <v-icon class="chevron" :class="{ open: setupOpen }" icon="mdi-chevron-down" size="18" />
        </button>
        <div v-show="setupOpen" class="nav-sublist">
          <button
            v-for="item in visibleSetupItems"
            :key="item.label"
            type="button"
            class="nav-subitem"
            :class="{ active: route.path === item.path }"
            @click="navigateTo(item.path)"
          >
            <v-icon :icon="item.icon" size="18" />
            <span>{{ item.label }}</span>
          </button>
        </div>
      </div>
    </nav>

    <button class="profile-card" type="button" :class="{ active: route.path === '/profile' }" @click="navigateTo('/profile')">
        <div class="avatar">{{ userInitials }}</div>
        <div class="profile-info">
        <div class="profile-name">{{ currentUser?.name || 'Fleet User' }}</div>
        <div class="profile-email">{{ currentUser?.role || currentUser?.email || '' }}</div>
      </div>
    </button>
  </div>
</template>

<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { canViewModule, getCurrentUser } from '../../utils/authSession'

const SIDEBAR_SCROLL_KEY = 'fleet-sidebar-scroll-top'

const route = useRoute()
const router = useRouter()
const sidebarRef = ref(null)

const menuItems = [
  { icon: 'mdi-view-dashboard', label: 'Dashboard', path: '/dashboard', module: 'dashboard' },
  { icon: 'mdi-truck', label: 'Vehicles', path: '/vehicles', module: 'vehicles' },
  { icon: 'mdi-map-marker', label: 'Trips', path: '/trips', module: 'trips' },
  { icon: 'mdi-file-document-outline', label: 'Reports', path: '/reports', module: 'reports' },
  { icon: 'mdi-cash-multiple', label: 'Expenses', path: '/expenses', module: 'expenses' }
]

const userItems = [
  { icon: 'mdi-account-multiple', label: 'Users', path: '/users', module: 'users' },
  { icon: 'mdi-shield-account', label: 'Roles', path: '/roles', module: 'roles' },
  { icon: 'mdi-shield-key-outline', label: 'Permissions', path: '/permissions', module: 'permissions' },
  { icon: 'mdi-history', label: 'Audit Logs', path: '/audit-logs', module: 'audit-logs' }
]

const setupItems = [
  { icon: 'mdi-domain', label: 'Department Setup', path: '/user-code-setup/departments', module: 'department-setup' },
  { icon: 'mdi-map-marker-multiple', label: 'Location Setup', path: '/user-code-setup/locations', module: 'location-setup' },
  { icon: 'mdi-map-marker-radius-outline', label: 'Location Type Setup', path: '/user-code-setup/location-types', module: 'location-type-setup' },
  { icon: 'mdi-truck-cargo-container', label: 'Vehicle Type Setup', path: '/user-code-setup/vehicle-types', module: 'vehicle-type-setup' },
  { icon: 'mdi-fuel', label: 'Fuel Type Setup', path: '/user-code-setup/fuel-types', module: 'fuel-type-setup' },
  { icon: 'mdi-map-marker-path', label: 'Trip Type Setup', path: '/user-code-setup/trip-types', module: 'trip-type-setup' },
  { icon: 'mdi-package-variant-closed', label: 'Cargo Type Setup', path: '/user-code-setup/cargo-types', module: 'cargo-type-setup' },
  { icon: 'mdi-list-status', label: 'Status Setup', path: '/user-code-setup/statuses', module: 'status-setup' },
  { icon: 'mdi-priority-high', label: 'Trip Priority Setup', path: '/user-code-setup/trip-priorities', module: 'trip-priority-setup' },
  { icon: 'mdi-alert-decagram-outline', label: 'Incident Type Setup', path: '/user-code-setup/incident-types', module: 'incident-type-setup' },
  { icon: 'mdi-alert-outline', label: 'Severity Setup', path: '/user-code-setup/severities', module: 'severity-setup' },
  { icon: 'mdi-cash-multiple', label: 'Expense Type Setup', path: '/user-code-setup/expense-types', module: 'expense-type-setup' },
  { icon: 'mdi-wrench-clock', label: 'Maintenance Type Setup', path: '/user-code-setup/maintenance-types', module: 'maintenance-type-setup' },
  { icon: 'mdi-file-certificate-outline', label: 'Document Type Setup', path: '/user-code-setup/document-types', module: 'document-type-setup' },
  { icon: 'mdi-storefront-outline', label: 'Supplier Setup', path: '/user-code-setup/suppliers', module: 'supplier-setup' }
]

const maintenanceItems = [
  { icon: 'mdi-wrench-outline', label: 'Tickets', path: '/maintenance', module: 'maintenance-tickets' },
  { icon: 'mdi-toolbox-outline', label: 'Inventory & Parts', path: '/maintenance/inventory', module: 'inventory-parts' },
  { icon: 'mdi-clipboard-alert-outline', label: 'Incidents', path: '/incidents', module: 'incidents' }
]

const isVisible = (item) => canViewModule(item.module)
const visibleMenuItems = computed(() => menuItems.filter(isVisible))
const visibleUserItems = computed(() => userItems.filter(isVisible))
const visibleSetupItems = computed(() => setupItems.filter(isVisible))
const visibleMaintenanceItems = computed(() => maintenanceItems.filter(isVisible))
const currentUser = computed(() => getCurrentUser())
const userInitials = computed(() =>
  String(currentUser.value?.name || 'FU')
    .split(' ')
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()
)

const isMaintenanceRoute = (path) => path.startsWith('/maintenance') || path === '/incidents'
const isUserManagementRoute = (path) =>
  path.startsWith('/users') || path.startsWith('/roles') || path.startsWith('/permissions') || path.startsWith('/audit-logs')

const maintenanceOpen = ref(isMaintenanceRoute(route.path))
const userOpen = ref(isUserManagementRoute(route.path))
const setupOpen = ref(route.path.startsWith('/user-code-setup'))

const saveSidebarScroll = () => {
  if (!sidebarRef.value) return
  sessionStorage.setItem(SIDEBAR_SCROLL_KEY, String(sidebarRef.value.scrollTop))
}

const restoreSidebarScroll = () => {
  nextTick(() => {
    requestAnimationFrame(() => {
      const storedScrollTop = Number(sessionStorage.getItem(SIDEBAR_SCROLL_KEY) || 0)
      if (sidebarRef.value) {
        sidebarRef.value.scrollTop = storedScrollTop
      }
    })
  })
}

const navigateTo = (path) => {
  saveSidebarScroll()
  if (route.path !== path) {
    router.push(path)
  }
}

watch(
  () => route.path,
  (path) => {
    if (isMaintenanceRoute(path)) {
      maintenanceOpen.value = true
    }
    if (isUserManagementRoute(path)) {
      userOpen.value = true
    }
    if (path.startsWith('/user-code-setup')) {
      setupOpen.value = true
    }
    restoreSidebarScroll()
  }
)

onMounted(() => {
  restoreSidebarScroll()
  sidebarRef.value?.addEventListener('scroll', saveSidebarScroll, { passive: true })
})

onBeforeUnmount(() => {
  saveSidebarScroll()
  sidebarRef.value?.removeEventListener('scroll', saveSidebarScroll)
})
</script>

<style scoped>
.sidebar-wrap {
  height: 100%;
  display: flex;
  flex-direction: column;
  padding: 16px;
  background: #fff;
  overflow-y: auto;
  overscroll-behavior: contain;
}

.logo-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 6px 16px;
  border-bottom: 1px solid var(--fleet-border);
  margin-bottom: 16px;
}

.logo-icon {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  display: grid;
  place-items: center;
  background: linear-gradient(135deg, #2563eb, #1e40af);
  color: #fff;
}

.logo-title {
  font-size: 16px;
  font-weight: 700;
  margin: 0;
}

.logo-subtitle {
  margin: 0;
  font-size: 12px;
  color: var(--fleet-muted);
}

.nav-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
  flex: 1;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 14px;
  border-radius: 12px;
  border: none;
  background: transparent;
  color: #334155;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s ease, color 0.2s ease;
  text-align: left;
}

.nav-item:hover {
  background: #f8fafc;
}

.nav-item span {
  flex: 1;
  text-align: left;
  white-space: nowrap;
}

.nav-item.active {
  background: #eff6ff;
  color: #1d4ed8;
}

.nav-group {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.chevron {
  margin-left: auto;
  transition: transform 0.2s ease;
  color: #94a3b8;
}

.chevron.open {
  transform: rotate(180deg);
}

.nav-sublist {
  display: flex;
  flex-direction: column;
  gap: 4px;
  padding-left: 12px;
}

.nav-subitem {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 10px 12px;
  border-radius: 10px;
  border: none;
  background: transparent;
  color: #475569;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s ease, color 0.2s ease;
  text-align: left;
}

.nav-subitem .v-icon {
  flex: 0 0 auto;
  margin-top: 1px;
}

.nav-subitem span {
  flex: 1;
  min-width: 0;
  line-height: 1.25;
  text-align: left;
}

.nav-subitem:hover {
  background: #f8fafc;
}

.nav-subitem.active {
  background: #eef2ff;
  color: #1d4ed8;
}

.profile-card {
  display: flex;
  gap: 12px;
  align-items: center;
  padding: 12px;
  border-top: 1px solid var(--fleet-border);
  border-right: none;
  border-bottom: none;
  border-left: none;
  background: transparent;
  cursor: pointer;
  width: 100%;
  text-align: left;
  border-radius: 12px;
}

.profile-card:hover,
.profile-card.active {
  background: #eff6ff;
}

.avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-weight: 700;
  color: #fff;
  background: linear-gradient(135deg, #2563eb, #1e40af);
}

.profile-name {
  font-weight: 600;
}

.profile-email {
  font-size: 12px;
  color: var(--fleet-muted);
}
</style>
