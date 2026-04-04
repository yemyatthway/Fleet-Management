<template>
  <div class="card-surface">
    <div class="table-wrap">
      <v-data-table
        class="table-base"
        :headers="headers"
        :items="users"
        :items-per-page="10"
        :items-per-page-options="[10, 20, 30]"
        :mobile-breakpoint="0"
        :mobile="false"
        fixed-header
        height="520"
        density="comfortable"
      >
        <template #item.name="{ item }">
          <div class="name-cell">
            <span class="user-id">{{ item.id }}</span>
            <button
              class="avatar avatar-button tooltip"
              type="button"
              @click="item.avatar && $emit('view-avatar', item)"
            >
              <img v-if="item.avatar" :src="item.avatar" :alt="item.name" />
              <span v-else>{{ initials(item.name) }}</span>
              <span v-if="item.avatar" class="tooltip-text">View profile image</span>
            </button>
            <strong>{{ item.name }}</strong>
          </div>
        </template>

        <template #item.employeeId="{ item }">
          <span class="text-muted">{{ item.employeeId }}</span>
        </template>

        <template #item.nrcNumber="{ item }">
          <span class="text-muted">{{ item.nrcNumber }}</span>
        </template>

        <template #item.email="{ item }">
          <span class="text-muted">{{ item.email }}</span>
        </template>

        <template #item.role="{ item }">
          <span class="role-pill" :class="roleClass(item.role)">{{ item.role }}</span>
        </template>

        <template #item.phone="{ item }">
          <span class="text-muted">{{ item.phone }}</span>
        </template>

        <template #item.department="{ item }">
          <span class="text-muted">{{ item.department }}</span>
        </template>

        <template #item.title="{ item }">
          <span class="text-muted">{{ item.title }}</span>
        </template>

        <template #item.location="{ item }">
          <span class="text-muted">{{ item.location }}</span>
        </template>

        <template #item.manager="{ item }">
          <span class="text-muted">{{ item.manager }}</span>
        </template>

        <template #item.status="{ item }">
          <span class="badge" :class="item.status === 'Active' ? 'success' : 'neutral'">
            {{ item.status }}
          </span>
        </template>

        <template #item.joinDate="{ item }">
          <span class="text-muted">{{ formatDate(item.joinDate) }}</span>
        </template>

        <template #item.lastLogin="{ item }">
          <span class="text-muted">{{ formatDateTime(item.lastLogin) }}</span>
        </template>

        <template #item.twoFactorEnabled="{ item }">
          <span class="badge" :class="item.twoFactorEnabled ? 'success' : 'neutral'">
            {{ item.twoFactorEnabled ? 'Enabled' : 'Off' }}
          </span>
        </template>

        <template #item.actions="{ item }">
          <div class="inline-actions">
            <button class="icon-button tooltip" @click="$emit('edit', item.id)">
              <v-icon icon="mdi-pencil-outline" size="18" />
              <span class="tooltip-text">Edit user</span>
            </button>
            <button
              class="icon-button tooltip"
              :class="item.status === 'Active' ? 'warn' : 'good'"
              @click="$emit('toggle', item.id)"
            >
              <v-icon icon="mdi-power" size="18" />
              <span class="tooltip-text">
                {{ item.status === 'Active' ? 'Disable user' : 'Enable user' }}
              </span>
            </button>
            <button class="icon-button danger tooltip" @click="$emit('remove', item.id)">
              <v-icon icon="mdi-trash-can-outline" size="18" />
              <span class="tooltip-text">Delete user</span>
            </button>
          </div>
        </template>

        <template #no-data>
          <div class="empty-state">No users found matching your criteria</div>
        </template>
      </v-data-table>
    </div>
  </div>
</template>

<script setup>
import { roleClassMap } from '../../data/roles'

defineProps({
  users: {
    type: Array,
    required: true
  }
})

defineEmits(['edit', 'toggle', 'remove', 'view-avatar'])

const headers = [
  { title: 'ID / Name', key: 'name' },
  { title: 'Employee ID', key: 'employeeId' },
  { title: 'NRC', key: 'nrcNumber' },
  { title: 'Email', key: 'email' },
  { title: 'Role', key: 'role' },
  { title: 'Phone', key: 'phone' },
  { title: 'Department', key: 'department' },
  { title: 'Title', key: 'title' },
  { title: 'Location', key: 'location' },
  { title: 'Manager', key: 'manager' },
  { title: 'Status', key: 'status' },
  { title: 'Join Date', key: 'joinDate' },
  { title: 'Last Login', key: 'lastLogin' },
  { title: '2FA', key: 'twoFactorEnabled' },
  { title: 'Actions', key: 'actions', align: 'end', sortable: false }
]

const roleClass = (role) => roleClassMap[role] || 'role-driver'

const initials = (name) => name.split(' ').map((part) => part[0]).join('')

const formatDate = (value) =>
  new Date(value).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })

const formatDateTime = (value) => {
  if (!value) return '—'
  return new Date(value).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit'
  })
}
</script>

<style scoped>
.table-wrap {
  overflow-x: auto;
}

.table-base {
  width: 100%;
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

.table-base :deep(tbody)::before,
.table-base :deep(tbody)::after {
  display: none;
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
  width: 130px;
}

.table-base :deep(thead th:nth-child(3)),
.table-base :deep(tbody td:nth-child(3)) {
  width: 190px;
}

.table-base :deep(thead th:nth-child(4)),
.table-base :deep(tbody td:nth-child(4)) {
  width: 260px;
}

.table-base :deep(thead th:nth-child(5)),
.table-base :deep(tbody td:nth-child(5)) {
  width: 140px;
}

.table-base :deep(thead th:nth-child(6)),
.table-base :deep(tbody td:nth-child(6)) {
  width: 180px;
}

.table-base :deep(thead th:nth-child(7)),
.table-base :deep(tbody td:nth-child(7)) {
  width: 160px;
}

.table-base :deep(thead th:nth-child(8)),
.table-base :deep(tbody td:nth-child(8)) {
  width: 180px;
}

.table-base :deep(thead th:nth-child(9)),
.table-base :deep(tbody td:nth-child(9)) {
  width: 160px;
}

.table-base :deep(thead th:nth-child(10)),
.table-base :deep(tbody td:nth-child(10)) {
  width: 160px;
}

.table-base :deep(thead th:nth-child(11)),
.table-base :deep(tbody td:nth-child(11)) {
  width: 120px;
}

.table-base :deep(thead th:nth-child(12)),
.table-base :deep(tbody td:nth-child(12)) {
  width: 140px;
}

.table-base :deep(thead th:nth-child(13)),
.table-base :deep(tbody td:nth-child(13)) {
  width: 180px;
}

.table-base :deep(thead th:nth-child(14)),
.table-base :deep(tbody td:nth-child(14)) {
  width: 90px;
}

.table-base :deep(thead th:nth-child(15)),
.table-base :deep(tbody td:nth-child(15)) {
  width: 140px;
}

.table-base :deep(thead th.align-right),
.table-base :deep(tbody td.align-right) {
  text-align: right;
}

.table-base thead th {
  position: sticky;
  top: 0;
  background: #fff;
  z-index: 1;
}

.align-right {
  text-align: right;
}

.name-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.user-id {
  font-weight: 700;
  color: #94a3b8;
  font-size: 12px;
  min-width: 28px;
  text-align: right;
}

.avatar {
  width: 38px;
  height: 38px;
  min-width: 38px;
  min-height: 38px;
  flex: 0 0 38px;
  border-radius: 50%;
  aspect-ratio: 1 / 1;
  display: grid;
  place-items: center;
  font-weight: 700;
  color: #fff;
  background: linear-gradient(135deg, #2563eb, #1e40af);
  overflow: hidden;
  border: none;
  padding: 0;
}

.avatar img {
  width: 100% !important;
  height: 100% !important;
  object-fit: cover;
  display: block;
  border-radius: 50%;
}

.avatar-button {
  cursor: pointer;
}

.avatar-button:disabled {
  cursor: default;
}

.role-pill {
  display: inline-flex;
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

.icon-button.warn {
  color: #ea580c;
}

.icon-button.warn:hover {
  background: #ffedd5;
}

.icon-button.good {
  color: #16a34a;
}

.icon-button.good:hover {
  background: #dcfce7;
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
