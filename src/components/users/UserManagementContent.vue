<template>
  <div class="user-page">
    <div>
      <h1 class="section-title">User Management</h1>
      <p class="section-subtitle">Manage system users and permissions</p>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Users</p>
        <h3>{{ users.length }}</h3>
      </div>
      <div class="stat-card">
        <p>Active Users</p>
        <h3 class="text-success">{{ activeCount }}</h3>
      </div>
      <div class="stat-card">
        <p>Drivers</p>
        <h3 class="text-info">{{ driverCount }}</h3>
      </div>
      <div class="stat-card">
        <p>Admins</p>
        <h3 class="text-purple">{{ adminCount }}</h3>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search by namse or email..."
          />
        </div>

        <div class="toolbar-actions">
          <div class="toolbar-filter">
            <v-icon icon="mdi-filter-variant" />
            <select v-model="roleFilter">
              <option value="All">All Roles</option>
              <option v-for="role in roleNames" :key="role" :value="role">
                {{ role }}
              </option>
            </select>
          </div>

          <button
            class="primary-button"
            type="button"
            @click="dialogOpen = true"
          >
            <v-icon icon="mdi-account-plus" size="18" />
            Add User
          </button>
        </div>
      </div>

      <div class="toolbar-count text-muted">
        Showing {{ filteredUsers.length }} of {{ users.length }} users
      </div>
    </div>

    <UserTable
      :users="filteredUsers"
      @edit="handleEdit"
      @toggle="handleToggle"
      @remove="handleDelete"
      @view-avatar="openAvatar"
    />

    <AddUserDialog
      :open="dialogOpen"
      @close="dialogOpen = false"
      @add="handleAdd"
    />

    <EditUserDialog
      :open="editOpen"
      :user="selectedUser"
      @close="editOpen = false"
      @save="handleUpdate"
    />

    <ConfirmDialog
      :open="confirmOpen"
      :title="confirmTitle"
      :message="confirmMessage"
      :confirm-text="confirmButton"
      :tone="confirmTone"
      @confirm="runConfirm"
      @cancel="confirmOpen = false"
    />

    <v-dialog v-model="avatarOpen" max-width="420">
      <v-card class="dialog-card">
        <div class="dialog-header">
          <h2>{{ avatarName }}</h2>
          <button class="icon-button" type="button" @click="avatarOpen = false">
            <v-icon icon="mdi-close" />
          </button>
        </div>
        <div class="dialog-body">
          <img
            v-if="avatarUrl"
            class="avatar-preview"
            :src="avatarUrl"
            :alt="avatarName"
          />
        </div>
      </v-card>
    </v-dialog>
  </div>
</template>

<script setup>
import { computed, ref } from "vue";
import UserTable from "./UserTable.vue";
import AddUserDialog from "./AddUserDialog.vue";
import EditUserDialog from "./EditUserDialog.vue";
import ConfirmDialog from "../common/ConfirmDialog.vue";
import { roleNames } from "../../data/roles";
import { users } from "../../data/users";

const searchQuery = ref("");
const roleFilter = ref("All");
const dialogOpen = ref(false);
const editOpen = ref(false);
const selectedUser = ref(null);
const confirmOpen = ref(false);
const confirmTitle = ref("Are you sure?");
const confirmMessage = ref("");
const confirmButton = ref("Confirm");
const confirmTone = ref("danger");
const pendingAction = ref(() => {});
const avatarOpen = ref(false);
const avatarUrl = ref("");
const avatarName = ref("");

const filteredUsers = computed(() => {
  const query = searchQuery.value.toLowerCase();
  return users.value.filter((user) => {
    const matchesSearch =
      user.name.toLowerCase().includes(query) ||
      user.email.toLowerCase().includes(query) ||
      user.nrcNumber?.toLowerCase().includes(query) ||
      user.employeeId?.toLowerCase().includes(query) ||
      user.phone?.toLowerCase().includes(query) ||
      user.department?.toLowerCase().includes(query) ||
      user.title?.toLowerCase().includes(query) ||
      user.location?.toLowerCase().includes(query);
    const matchesRole =
      roleFilter.value === "All" || user.role === roleFilter.value;
    return matchesSearch && matchesRole;
  });
});

const activeCount = computed(
  () => users.value.filter((u) => u.status === "Active").length,
);
const driverCount = computed(
  () => users.value.filter((u) => u.role === "Driver").length,
);
const adminCount = computed(
  () => users.value.filter((u) => u.role === "Admin").length,
);

const handleAdd = (payload) => {
  users.value.push({
    ...payload,
    id: String(users.value.length + 1),
    joinDate: new Date().toISOString().split("T")[0],
    lastLogin: new Date().toISOString(),
  });
  dialogOpen.value = false;
};

const handleEdit = (id) => {
  const user = users.value.find((item) => item.id === id);
  if (!user) return;
  selectedUser.value = { ...user };
  editOpen.value = true;
};

const openConfirm = ({ title, message, confirmText, tone, action }) => {
  confirmTitle.value = title;
  confirmMessage.value = message;
  confirmButton.value = confirmText;
  confirmTone.value = tone;
  pendingAction.value = action;
  confirmOpen.value = true;
};

const runConfirm = () => {
  pendingAction.value();
  confirmOpen.value = false;
};

const openAvatar = (user) => {
  if (!user?.avatar) return;
  avatarUrl.value = user.avatar;
  avatarName.value = user.name;
  avatarOpen.value = true;
};

const handleToggle = (id) => {
  const user = users.value.find((item) => item.id === id);
  if (!user) return;
  const nextStatus = user.status === "Active" ? "Disabled" : "Active";
  openConfirm({
    title: `${nextStatus} User?`,
    message: `This will mark ${user.name} as ${nextStatus.toLowerCase()}.`,
    confirmText: nextStatus,
    tone: "warning",
    action: () => {
      users.value = users.value.map((item) =>
        item.id === id ? { ...item, status: nextStatus } : item,
      );
    },
  });
};

const handleDelete = (id) => {
  const user = users.value.find((item) => item.id === id);
  if (!user) return;
  openConfirm({
    title: "Delete User?",
    message: `This will permanently remove ${user.name}.`,
    confirmText: "Delete",
    tone: "danger",
    action: () => {
      users.value = users.value.filter((item) => item.id !== id);
    },
  });
};

const handleUpdate = (payload) => {
  users.value = users.value.map((item) =>
    item.id === payload.id ? { ...item, ...payload } : item,
  );
  editOpen.value = false;
};
</script>

<style scoped>
.user-page {
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
  gap: 16px;
}

.stat-card {
  padding: 16px;
  border-radius: 14px;
  border: 1px solid var(--fleet-border);
  background: #fff;
}

.stat-card p {
  margin: 0;
  font-size: 13px;
  color: var(--fleet-muted);
}

.stat-card h3 {
  margin: 8px 0 0;
  font-size: 22px;
}

.text-success {
  color: var(--fleet-success);
}

.text-info {
  color: var(--fleet-primary);
}

.text-purple {
  color: #7c3aed;
}

.toolbar {
  padding: 18px;
}

.toolbar-row {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
}

.toolbar-search,
.toolbar-filter {
  display: flex;
  align-items: center;
  gap: 8px;
  border: 1px solid var(--fleet-border);
  border-radius: 12px;
  padding: 10px 12px;
  background: #fff;
  min-width: 260px;
}

.toolbar-search input,
.toolbar-filter select {
  border: none;
  outline: none;
  background: transparent;
  font-size: 14px;
  width: 100%;
}

.toolbar-filter select {
  appearance: none;
  cursor: pointer;
}

.toolbar-filter {
  cursor: pointer;
}

.toolbar-search {
  flex: 1;
  min-width: 320px;
}

.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}

@media (max-width: 720px) {
  .toolbar-row {
    flex-direction: column;
    align-items: stretch;
  }

  .toolbar-search {
    width: 100%;
  }

  .toolbar-actions {
    width: 100%;
    flex-direction: column;
    align-items: stretch;
  }

  .toolbar-filter {
    width: 100%;
  }

  .primary-button {
    width: 100%;
    justify-content: center;
  }
}

.primary-button {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  border: none;
  border-radius: 12px;
  padding: 10px 16px;
  background: var(--fleet-primary);
  color: #fff;
  font-weight: 600;
  cursor: pointer;
}

.primary-button:hover {
  background: var(--fleet-primary-dark);
}

.dialog-card {
  border-radius: 16px;
  padding: 0;
}

.dialog-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 20px 24px;
  border-bottom: 1px solid var(--fleet-border);
}

.dialog-header h2 {
  margin: 0;
  font-size: 18px;
}

.dialog-body {
  padding: 20px 24px 24px;
  display: grid;
  place-items: center;
}

.avatar-preview {
  width: 100%;
  max-width: 320px;
  border-radius: 16px;
  object-fit: cover;
}

.toolbar-count {
  margin-top: 12px;
  font-size: 13px;
}
</style>
