<template>
  <div class="card-surface">
    <div class="table-wrap">
      <table class="table-base">
        <thead>
          <tr>
            <th>Name</th>
            <th>Email</th>
            <th>Role</th>
            <th>Phone</th>
            <th>Status</th>
            <th>Join Date</th>
            <th class="align-right">Actions</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="user in users" :key="user.id">
            <td>
              <div class="name-cell">
                <div class="avatar">{{ initials(user.name) }}</div>
                <strong>{{ user.name }}</strong>
              </div>
            </td>
            <td class="text-muted">{{ user.email }}</td>
            <td>
              <span class="role-pill" :class="roleClass(user.role)">{{ user.role }}</span>
            </td>
            <td class="text-muted">{{ user.phone }}</td>
            <td>
              <span class="badge" :class="user.status === 'Active' ? 'success' : 'neutral'">
                {{ user.status }}
              </span>
            </td>
            <td class="text-muted">{{ formatDate(user.joinDate) }}</td>
            <td class="align-right">
              <div class="inline-actions">
                <button class="icon-button" @click="$emit('edit', user.id)">
                  <v-icon icon="mdi-pencil-outline" size="18" />
                </button>
                <button class="icon-button" :class="user.status === 'Active' ? 'warn' : 'good'" @click="$emit('toggle', user.id)">
                  <v-icon icon="mdi-power" size="18" />
                </button>
                <button class="icon-button danger" @click="$emit('remove', user.id)">
                  <v-icon icon="mdi-trash-can-outline" size="18" />
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-if="users.length === 0" class="empty-state">
      No users found matching your criteria
    </div>
  </div>
</template>

<script setup>
defineProps({
  users: {
    type: Array,
    required: true
  }
})

defineEmits(['edit', 'toggle', 'remove'])

const roleClass = (role) => {
  const map = {
    Admin: 'role-admin',
    Dispatcher: 'role-dispatcher',
    Driver: 'role-driver',
    Mechanic: 'role-mechanic'
  }
  return map[role] || 'role-driver'
}

const initials = (name) => name.split(' ').map((part) => part[0]).join('')

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

.align-right {
  text-align: right;
}

.name-cell {
  display: flex;
  align-items: center;
  gap: 12px;
}

.avatar {
  width: 38px;
  height: 38px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  font-weight: 700;
  color: #fff;
  background: linear-gradient(135deg, #2563eb, #1e40af);
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

.empty-state {
  text-align: center;
  padding: 32px;
  color: var(--fleet-muted);
}
</style>
