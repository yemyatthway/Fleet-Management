<template>
  <DashboardLayout>
    <TripSetupContent
      title="Trip Priority Setup"
      subtitle="Manage trip priority values used for dispatch planning."
      label="Trip Priority"
      code-prefix="TPR"
      module-key="trip-priority-setup"
      :api="tripPrioritiesApi"
    />
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from "../layouts/DashboardLayout.vue";
import TripSetupContent from "../components/tripSetup/TripSetupContent.vue";
import { tripPrioritiesApi } from "../services/tripSetupApi";
</script>
