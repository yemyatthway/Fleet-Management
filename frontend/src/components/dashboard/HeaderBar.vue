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
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { clearAuthSession } from '../../utils/authSession'

defineEmits(['toggle'])

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
