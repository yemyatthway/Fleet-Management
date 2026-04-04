<template>
  <div class="sidebar-wrap">
    <div class="logo-row">
      <div class="logo-icon">
        <v-icon icon="mdi-truck" size="24" />
      </div>
      <div>
        <h1 class="logo-title">FleetManager</h1>
        <p class="logo-subtitle">Admin Portal</p>
      </div>
    </div>

    <nav class="nav-list">
      <button
        v-for="item in menuItems"
        :key="item.label"
        type="button"
        class="nav-item"
        :class="{ active: route.path === item.path }"
        @click="router.push(item.path)"
      >
        <v-icon :icon="item.icon" size="20" />
        <span>{{ item.label }}</span>
      </button>

      <div class="nav-group">
        <button
          type="button"
          class="nav-item nav-group-toggle"
          :class="{ active: route.path.startsWith('/maintenance') }"
          @click="maintenanceOpen = !maintenanceOpen"
        >
          <v-icon icon="mdi-alert-circle-outline" size="20" />
          <span>Maintenance</span>
          <v-icon class="chevron" :class="{ open: maintenanceOpen }" icon="mdi-chevron-down" size="18" />
        </button>
        <div v-show="maintenanceOpen" class="nav-sublist">
          <button
            v-for="item in maintenanceItems"
            :key="item.label"
            type="button"
            class="nav-subitem"
            :class="{ active: route.path === item.path }"
            @click="router.push(item.path)"
          >
            <v-icon :icon="item.icon" size="18" />
            <span>{{ item.label }}</span>
          </button>
        </div>
      </div>
    </nav>

    <div class="profile-card">
      <div class="avatar">AD</div>
      <div class="profile-info">
        <div class="profile-name">Admin User</div>
        <div class="profile-email">admin@fleet.com</div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()

const menuItems = [
  { icon: 'mdi-view-dashboard', label: 'Dashboard', path: '/dashboard' },
  { icon: 'mdi-truck', label: 'Vehicles', path: '/vehicles' },
  { icon: 'mdi-map-marker', label: 'Trips', path: '/trips' },
  { icon: 'mdi-chart-box', label: 'Analytics', path: '/analytics' },
  { icon: 'mdi-file-document-outline', label: 'Reports', path: '/reports' },
  { icon: 'mdi-cog', label: 'Settings', path: '/settings' }
]

const userItems = [
  { icon: 'mdi-account-multiple', label: 'Users', path: '/users' },
  { icon: 'mdi-shield-account', label: 'Roles', path: '/roles' }
]

const maintenanceItems = [
  { icon: 'mdi-wrench-outline', label: 'Tickets', path: '/maintenance' },
  { icon: 'mdi-toolbox-outline', label: 'Inventory & Parts', path: '/maintenance/inventory' }
]

const maintenanceOpen = ref(route.path.startsWith('/maintenance'))
const userOpen = ref(route.path.startsWith('/users') || route.path.startsWith('/roles'))

watch(
  () => route.path,
  (path) => {
    if (path.startsWith('/maintenance')) {
      maintenanceOpen.value = true
    }
    if (path.startsWith('/users') || path.startsWith('/roles')) {
      userOpen.value = true
    }
  }
)
</script>

<style scoped>
.sidebar-wrap {
  height: 100%;
  display: flex;
  flex-direction: column;
  padding: 16px;
  background: #fff;
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
}

.nav-item:hover {
  background: #f8fafc;
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
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  border-radius: 10px;
  border: none;
  background: transparent;
  color: #475569;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.2s ease, color 0.2s ease;
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
