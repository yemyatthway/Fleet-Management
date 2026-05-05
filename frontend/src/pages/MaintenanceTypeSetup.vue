<template>
  <DashboardLayout>
    <TripSetupContent
      title="Maintenance Type Setup"
      subtitle="Manage maintenance categories for tickets and service records."
      label="Maintenance Type"
      code-prefix="MNT"
      module-key="maintenance-type-setup"
      :api="maintenanceTypesApi"
    />
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from '../layouts/DashboardLayout.vue'
import TripSetupContent from '../components/tripSetup/TripSetupContent.vue'
import { maintenanceTypesApi } from '../services/tripSetupApi'
</script>
