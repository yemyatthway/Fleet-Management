<template>
  <DashboardLayout>
    <TripSetupContent
      title="Severity Setup"
      subtitle="Manage severity levels used by incidents and priority workflows."
      label="Severity"
      code-prefix="SEV"
      module-key="severity-setup"
      :api="severitiesApi"
    />
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from '../layouts/DashboardLayout.vue'
import TripSetupContent from '../components/tripSetup/TripSetupContent.vue'
import { severitiesApi } from '../services/tripSetupApi'
</script>
