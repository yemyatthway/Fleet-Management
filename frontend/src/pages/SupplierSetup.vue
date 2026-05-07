<template>
  <DashboardLayout>
    <TripSetupContent
      title="Supplier Setup"
      subtitle="Manage approved suppliers for inventory and spare parts."
      label="Supplier"
      code-prefix="SUP"
      module-key="supplier-setup"
      :api="suppliersApi"
    />
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from "../layouts/DashboardLayout.vue";
import TripSetupContent from "../components/tripSetup/TripSetupContent.vue";
import { suppliersApi } from "../services/tripSetupApi";
</script>
