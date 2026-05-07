<template>
  <DashboardLayout>
    <TripSetupContent
      title="Expense Type Setup"
      subtitle="Manage expense categories for fuel, repairs, tolls, and other fleet costs."
      label="Expense Type"
      code-prefix="EXT"
      module-key="expense-type-setup"
      :api="expenseTypesApi"
    />
  </DashboardLayout>
</template>

<script setup>
import DashboardLayout from "../layouts/DashboardLayout.vue";
import TripSetupContent from "../components/tripSetup/TripSetupContent.vue";
import { expenseTypesApi } from "../services/tripSetupApi";
</script>
