<template>
  <div class="card-surface">
    <div class="table-wrap">
      <v-data-table
        class="table-base"
        :headers="headers"
        :items="roles"
        :items-per-page="10"
        :items-per-page-options="[10, 20, 30]"
        :mobile-breakpoint="0"
        :mobile="false"
        fixed-header
        height="520"
        density="comfortable"
      >
        <template #item.role="{ item }">
          <div class="role-cell">
            <div class="role-badge" :class="roleClass(item.name)">{{ item.name }}</div>
          </div>
        </template>

        <template #item.description="{ item }">
          <span class="text-muted">{{ item.description }}</span>
        </template>


        <template #item.members="{ item }">
          <strong>{{ item.members }}</strong>
        </template>

        <template #item.view="{ item }">
          <button class="icon-button tooltip" type="button" @click="$emit('view', item)">
            <v-icon icon="mdi-eye-outline" size="18" />
            <span class="tooltip-text">View members</span>
          </button>
        </template>

        <template #item.actions="{ item }">
          <div class="inline-actions">
            <button class="icon-button tooltip" type="button" @click="$emit('edit', item)">
              <v-icon icon="mdi-pencil-outline" size="18" />
              <span class="tooltip-text">Edit role</span>
            </button>
            <button class="icon-button danger tooltip" type="button" @click="$emit('remove', item)">
              <v-icon icon="mdi-trash-can-outline" size="18" />
              <span class="tooltip-text">Delete role</span>
            </button>
          </div>
        </template>

        <template #item.createdAt="{ item }">
          <span class="text-muted">{{ formatDate(item.createdAt || item.updatedAt) }}</span>
        </template>

        <template #no-data>
          <div class="empty-state">No roles found matching your criteria</div>
        </template>
      </v-data-table>
    </div>
  </div>
</template>

<script setup>
import { roleClassMap } from '../../data/roles'

defineProps({
  roles: {
    type: Array,
    required: true
  }
})

defineEmits(['view', 'edit', 'remove'])

const headers = [
  { title: 'Role', key: 'role' },
  { title: 'Description', key: 'description' },
  { title: 'Members', key: 'members' },
  { title: 'View', key: 'view', align: 'end', sortable: false },
  { title: 'Actions', key: 'actions', align: 'end', sortable: false },
  { title: 'Created At', key: 'createdAt' }
]

const roleClass = (role) => roleClassMap[role] || 'role-driver'

const formatDate = (value) =>
  new Date(value).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })
</script>

<style scoped>
.table-wrap {
  overflow-x: auto;
}

.table-base {
  width: 100%;
}

.table-base :deep(.v-table__wrapper) {
  background: #fff;
}

.table-base :deep(table) {
  border-collapse: separate;
  border-spacing: 0;
}

.table-base :deep(thead th) {
  background: #f8fafc;
  color: #475569;
  font-size: 13px;
  letter-spacing: 0.02em;
  text-transform: uppercase;
  font-weight: 700;
  padding: 14px 16px;
}

.table-base :deep(tbody td) {
  padding: 14px 16px;
  background: #fff;
}

.table-base :deep(tbody tr) {
  box-shadow: 0 1px 2px rgba(15, 23, 42, 0.06);
}

.table-base :deep(tbody tr td) {
  border-bottom: 10px solid transparent;
}

.table-base :deep(tbody tr:last-child td) {
  border-bottom: 0;
}

.table-base :deep(tbody tr:nth-child(even) td) {
  background: #f8fafc;
}

.table-base :deep(tbody tr td:first-child) {
  border-radius: 12px 0 0 12px;
}

.table-base :deep(tbody tr td:last-child) {
  border-radius: 0 12px 12px 0;
}

.table-base :deep(thead th:first-child) {
  border-radius: 12px 0 0 12px;
}

.table-base :deep(thead th:last-child) {
  border-radius: 0 12px 12px 0;
}

.table-base :deep(thead th:nth-child(1)),
.table-base :deep(tbody td:nth-child(1)) {
  width: 220px;
}

.table-base :deep(thead th:nth-child(2)),
.table-base :deep(tbody td:nth-child(2)) {
  width: 320px;
}

.table-base :deep(thead th:nth-child(3)),
.table-base :deep(tbody td:nth-child(3)) {
  width: 140px;
}

.table-base :deep(thead th:nth-child(4)),
.table-base :deep(tbody td:nth-child(4)) {
  width: 120px;
}

.table-base :deep(thead th:nth-child(5)),
.table-base :deep(tbody td:nth-child(5)) {
  width: 160px;
}

.table-base :deep(thead th:nth-child(6)),
.table-base :deep(tbody td:nth-child(6)) {
  width: 160px;
}

.table-base :deep(thead th.align-right),
.table-base :deep(tbody td.align-right) {
  text-align: right;
}

.align-right {
  text-align: right;
}

.role-cell {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.role-badge {
  display: inline-flex;
  width: fit-content;
  padding: 4px 10px;
  border-radius: 999px;
  font-weight: 600;
  font-size: 12px;
}

.role-admin {
  background: #ede9fe;
  color: #6d28d9;
}

.role-dispatcher {
  background: #dbeafe;
  color: #1d4ed8;
}

.role-driver {
  background: #dcfce7;
  color: #15803d;
}

.role-mechanic {
  background: #ffedd5;
  color: #c2410c;
}


.icon-button {
  border: none;
  background: transparent;
  width: 34px;
  height: 34px;
  border-radius: 10px;
  cursor: pointer;
  color: #2563eb;
}

.icon-button:hover {
  background: #eff6ff;
}

.inline-actions {
  display: flex;
  justify-content: flex-end;
  gap: 6px;
}

.icon-button.danger {
  color: #dc2626;
}

.icon-button.danger:hover {
  background: #fee2e2;
}

.tooltip {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
}

.tooltip:focus-visible {
  outline: 2px solid rgba(37, 99, 235, 0.35);
  outline-offset: 2px;
}

.tooltip-text {
  position: absolute;
  bottom: calc(100% + 8px);
  left: auto;
  right: 0;
  transform: translate(0, 6px);
  background: #0f172a;
  color: #fff;
  padding: 6px 8px;
  border-radius: 8px;
  font-size: 12px;
  white-space: nowrap;
  opacity: 0;
  pointer-events: none;
  transition: opacity 0.15s ease, transform 0.15s ease;
  box-shadow: 0 8px 16px rgba(15, 23, 42, 0.2);
  z-index: 2;
}

.tooltip:hover .tooltip-text,
.tooltip:focus-visible .tooltip-text {
  opacity: 1;
  transform: translate(0, 0);
}

.table-base :deep(.v-data-table__td-title),
.table-base :deep(.v-data-table__mobile-row__header),
.table-base :deep(.v-data-table__mobile-row__header__title) {
  display: none !important;
}

.table-base :deep(.v-data-table__td-value) {
  padding-top: 0;
}

.table-base :deep(.v-data-table__mobile-row),
.table-base :deep(.v-data-table__row) {
  display: table-row !important;
}

.table-base :deep(.v-data-table__mobile-row__cell),
.table-base :deep(.v-data-table__td) {
  display: table-cell !important;
}
.empty-state {
  text-align: center;
  padding: 32px;
  color: var(--fleet-muted);
}
</style>
