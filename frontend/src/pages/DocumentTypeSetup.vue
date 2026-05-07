<template>
  <DashboardLayout>
    <TripSetupContent
      title="Document Type Setup"
      subtitle="Manage document categories for vehicles, drivers, and compliance records."
      label="Document Type"
      code-prefix="DCT"
      module-key="document-type-setup"
      :api="documentTypesApi"
    />
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from "../layouts/DashboardLayout.vue";
import TripSetupContent from "../components/tripSetup/TripSetupContent.vue";
import { documentTypesApi } from "../services/tripSetupApi";
</script>
