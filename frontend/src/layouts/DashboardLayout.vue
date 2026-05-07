<template>
  <v-layout class="dashboard-shell">
    <v-navigation-drawer
      v-model="drawer"
      :permanent="!isMobile"
      :temporary="isMobile"
      width="260"
      class="sidebar"
      floating
    >
      <SidebarNav />
    </v-navigation-drawer>

    <v-main class="main-column">
      <HeaderBar @toggle="drawer = !drawer" />
      <div class="page-shell">
        <slot />
      </div>
    </v-main>
  </v-layout>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useDisplay } from 'vuetify'
import SidebarNav from '../components/dashboard/SidebarNav.vue'
import HeaderBar from '../components/dashboard/HeaderBar.vue'

const drawer = ref(true)
const { mobile } = useDisplay()
const isMobile = mobile

watch(
  () => isMobile.value,
  (value) => {
    drawer.value = !value
  },
  { immediate: true }
)
</script>

<style scoped src="./layout_styles/DashboardLayout.css"></style>
