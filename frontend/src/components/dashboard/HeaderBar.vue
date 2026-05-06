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

<style scoped src="./dashboard_styles/HeaderBar.css"></style>
