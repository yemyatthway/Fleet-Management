<template>
  <DashboardLayout>
    <TripSetupContent
      title="Status Setup"
      subtitle="Manage reusable status values for operational workflows."
      label="Status"
      code-prefix="STS"
      module-key="status-setup"
      :api="statusesApi"
    />
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from "../layouts/DashboardLayout.vue";
import TripSetupContent from "../components/tripSetup/TripSetupContent.vue";
import { statusesApi } from "../services/tripSetupApi";
</script>
