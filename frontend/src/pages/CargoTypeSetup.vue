<template>
  <DashboardLayout>
    <TripSetupContent
      title="Cargo Type Setup"
      subtitle="Manage cargo type master data used by trips."
      label="Cargo Type"
      code-prefix="CGT"
      module-key="cargo-type-setup"
      :api="cargoTypesApi"
    />
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from "../layouts/DashboardLayout.vue";
import TripSetupContent from "../components/tripSetup/TripSetupContent.vue";
import { cargoTypesApi } from "../services/tripSetupApi";
</script>
