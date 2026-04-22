<template>
  <DashboardLayout>
    <UserCodeSetupContent
      section-title="Department Setup"
      section-subtitle="Manage department master data used by user forms."
      fixed-type="Department"
      item-label="Department"
      item-label-plural="Departments"
    />
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from '../layouts/DashboardLayout.vue'
import UserCodeSetupContent from '../components/users/UserCodeSetupContent.vue'
</script>
