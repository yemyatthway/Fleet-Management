<template>
  <DashboardLayout>
    <TripSetupContent
      title="Incident Type Setup"
      subtitle="Manage incident categories used by incident records."
      label="Incident Type"
      code-prefix="ICT"
      module-key="incident-type-setup"
      :api="incidentTypesApi"
    />
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from '../layouts/DashboardLayout.vue'
import TripSetupContent from '../components/tripSetup/TripSetupContent.vue'
import { incidentTypesApi } from '../services/tripSetupApi'
</script>
