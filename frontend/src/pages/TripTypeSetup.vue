<template>
  <DashboardLayout>
    <TripSetupContent
      title="Trip Type Setup"
      subtitle="Manage trip type master data for dispatch workflows."
      label="Trip Type"
      code-prefix="TTP"
      module-key="trip-type-setup"
      :api="tripTypesApi"
    />
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from '../layouts/DashboardLayout.vue'
import TripSetupContent from '../components/tripSetup/TripSetupContent.vue'
import { tripTypesApi } from '../services/tripSetupApi'
</script>
