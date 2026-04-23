<template>
  <DashboardLayout>
    <UserCodeSetupContent
      section-title="Location / Depot Setup"
      section-subtitle="Manage location and depot master data for vehicles and operations."
      fixed-type="Location"
      item-label="Location"
      item-label-plural="Locations"
    />
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from '../layouts/DashboardLayout.vue'
import UserCodeSetupContent from '../components/users/UserCodeSetupContent.vue'
</script>
