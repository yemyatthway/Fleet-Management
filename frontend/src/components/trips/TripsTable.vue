<template>
  <div class="trips-table">
    <!-- Table for displaying trips -->
    <table>
      <thead>
        <tr>
          <th>Trip ID</th>
          <th>Driver</th>
          <th>Vehicle</th>
          <th>Department</th>
          <th>Origin</th>
          <th>Destination</th>
          <th>Start Time</th>
          <th>End Time</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="trip in items" :key="trip.id">
          <td>{{ trip.displayId || trip.id }}</td>
          <td>{{ trip.driverName }}</td>
          <td>{{ trip.vehicleName }}</td>
          <td>{{ trip.departmentName }}</td>
          <td>{{ trip.origin }}</td>
          <td>{{ trip.destination }}</td>
          <td>{{ trip.startTime }}</td>
          <td>{{ trip.endTime }}</td>
          <td>{{ trip.status }}</td>
          <td>
            <button @click="$emit('edit', trip.id)">Edit</button>
            <button @click="$emit('remove', trip.id)">Delete</button>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script setup>
const props = defineProps({
  items: Array,
  total: Number,
  loading: Boolean,
  page: Number,
  itemsPerPage: Number,
  sortBy: String,
  sortOrder: String,
});
</script>

<style scoped>
.trips-table {
  margin-top: 2rem;
}
table {
  width: 100%;
  border-collapse: collapse;
}
th,
td {
  padding: 0.5rem 1rem;
  border: 1px solid #eee;
  text-align: left;
}
th {
  background: #f8f8f8;
}
</style>
