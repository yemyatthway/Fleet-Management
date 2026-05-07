<template>
  <v-app-bar flat height="64" class="header-bar" position="static">
    <div class="header-left">
      <v-btn icon variant="text" class="icon-button" @click="$emit('toggle')">
        <v-icon icon="mdi-menu" />
      </v-btn>
    </div>

    <div class="header-right">
      <div class="date-label">{{ today }}</div>
      <button class="logout-button" type="button" @click="logout">
        <v-icon icon="mdi-logout" size="18" />
        Logout
      </button>
    </div>
  </v-app-bar>

  <ConfirmDialog
    :open="logoutConfirmOpen"
    title="Logout?"
    message="You will be signed out of FleetManager and returned to the login page."
    confirm-text="Logout"
    cancel-text="Stay"
    tone="warning"
    icon="mdi-logout"
    @confirm="confirmLogout"
    @cancel="logoutConfirmOpen = false"
  />
</template>

<script setup>
import { computed, ref } from 'vue'
import { useRouter } from 'vue-router'
import ConfirmDialog from '../common/ConfirmDialog.vue'
import { clearAuthSession } from '../../utils/authSession'

defineEmits(['toggle'])

const router = useRouter()
const logoutConfirmOpen = ref(false)

const today = computed(() =>
  new Date().toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric'
  })
)

const logout = () => {
  logoutConfirmOpen.value = true
}

const confirmLogout = () => {
  logoutConfirmOpen.value = false
  clearAuthSession()
  router.push('/login')
}
</script>

<style scoped src="./dashboard_styles/HeaderBar.css"></style>
