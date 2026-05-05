<template>
  <v-app-bar flat height="64" class="header-bar" position="static">
    <div class="header-left">
      <v-btn icon variant="text" class="icon-button" @click="$emit('toggle')">
        <v-icon icon="mdi-menu" />
      </v-btn>
    </div>

    <div class="header-right">
      <v-menu
        v-model="menuOpen"
        location="bottom end"
        offset="12"
        :close-on-content-click="false"
      >
        <template #activator="{ props }">
          <button
            class="notify-button"
            :class="{ 'is-active': menuOpen }"
            type="button"
            v-bind="props"
          >
            <v-icon :icon="menuOpen ? 'mdi-bell' : 'mdi-bell-outline'" />
            <span class="notify-dot" :class="{ 'is-hidden': menuOpen }"></span>
          </button>
        </template>
        <div class="notify-menu">
          <NotificationsPanel @view-all="menuOpen = false" />
        </div>
      </v-menu>
      <div class="date-label">{{ today }}</div>
      <button class="logout-button" type="button" @click="logout">
        <v-icon icon="mdi-logout" size="18" />
        Logout
      </button>
    </div>
  </v-app-bar>
</template>

<script setup>
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import NotificationsPanel from './NotificationsPanel.vue'
import { clearAuthSession } from '../../utils/authSession'

defineEmits(['toggle'])

const menuOpen = ref(false)
const router = useRouter()

const today = computed(() =>
  new Date().toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric'
  })
)

const logout = () => {
  clearAuthSession()
  router.push('/login')
}
</script>

<style scoped>
.header-bar {
  background: #fff;
  border-bottom: 1px solid var(--fleet-border);
  padding: 0 24px;
}

.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.header-left {
  flex: 1;
}

.icon-button {
  border-radius: 12px;
}

.notify-button {
  position: relative;
  border: none;
  background: #fff;
  width: 40px;
  height: 40px;
  border-radius: 12px;
  display: grid;
  place-items: center;
  cursor: pointer;
  transition: background 0.2s ease, box-shadow 0.2s ease;
}

.notify-button:hover {
  background: #f8fafc;
}

.notify-button.is-active {
  background: #eef2ff;
  box-shadow: 0 6px 18px rgba(99, 102, 241, 0.18);
}

.notify-dot {
  position: absolute;
  top: 9px;
  right: 10px;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--fleet-danger);
}

.notify-dot.is-hidden {
  display: none;
}

.notify-menu {
  width: 360px;
  max-width: calc(100vw - 32px);
}

.notify-menu :deep(.card-surface) {
  box-shadow: 0 18px 40px rgba(15, 23, 42, 0.18);
}

.notify-menu :deep(.panel-list) {
  max-height: 360px;
  overflow-y: auto;
}

.date-label {
  color: var(--fleet-muted);
  font-size: 14px;
}

.logout-button {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  border: 1px solid var(--fleet-border);
  border-radius: 10px;
  background: #fff;
  color: #334155;
  cursor: pointer;
  font-size: 13px;
  font-weight: 700;
  padding: 9px 11px;
}

.logout-button:hover {
  background: #f8fafc;
}

@media (max-width: 720px) {
  .header-bar {
    padding: 0 16px;
  }

  .header-right {
    gap: 10px;
  }

  .date-label {
    display: none;
  }

  .logout-button {
    padding: 9px;
  }

  .logout-button .v-icon + * {
    display: none;
  }
}

</style>
