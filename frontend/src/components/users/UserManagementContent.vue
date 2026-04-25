<template>
  <div class="user-page">
    <div>
      <h1 class="section-title">User Management</h1>
      <p class="section-subtitle">Manage system users and permissions</p>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Users</p>
        <h3>{{ userStats.total }}</h3>
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
            placeholder="Search by name or email..."
          />
          <button
            v-if="searchQuery"
            class="clear-button"
            type="button"
            aria-label="Clear search"
            @click="searchQuery = ''"
          >
            <v-icon icon="mdi-close-circle" size="18" />
          </button>
        </div>

        <div class="toolbar-actions">
          <div class="toolbar-filter">
            <v-icon icon="mdi-filter-variant" />
            <select v-model="roleFilter">
              <option value="All">All Roles</option>
              <option v-for="role in userRoles" :key="role" :value="role">
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
        {{ loadingUsers ? 'Loading users...' : `Showing ${users.length} of ${totalUsers} users` }}
      </div>
    </div>

    <PageMessage
      :tone="pageMessage.tone"
      :title="pageMessage.title"
      :message="pageMessage.message"
      @close="clearPageMessage"
    />

    <UserTable
      :users="tableUsers"
      :total="totalUsers"
      :loading="loadingUsers"
      :page="tableOptions.page"
      :items-per-page="tableOptions.itemsPerPage"
      :sort-by="tableOptions.sortBy"
      :sort-order="tableOptions.sortOrder"
      @update:options="handleTableOptions"
      @edit="handleEdit"
      @toggle="handleToggle"
      @remove="handleDelete"
      @view-avatar="openAvatar"
    />

    <AddUserDialog
      :open="dialogOpen"
      :roles="userRoles"
      :departments="departmentOptions"
      @close="dialogOpen = false"
      @add="handleAdd"
    />

    <EditUserDialog
      :open="editOpen"
      :user="selectedUser"
      :roles="userRoles"
      :departments="departmentOptions"
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

    <UserAvatarDialog
      :open="avatarOpen"
      :name="avatarName"
      :url="avatarUrl"
      @update:open="avatarOpen = $event"
    />
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from "vue";
import UserTable from "./UserTable.vue";
import AddUserDialog from "./AddUserDialog.vue";
import EditUserDialog from "./EditUserDialog.vue";
import UserAvatarDialog from "./UserAvatarDialog.vue";
import ConfirmDialog from "../common/ConfirmDialog.vue";
import PageMessage from "../common/PageMessage.vue";
import { useConfirmDialog } from "../../composables/useConfirmDialog";
import { useListPage } from "../../composables/useListPage";
import { usePageMessage } from "../../composables/usePageMessage";
import { attachDisplayIds } from "../../utils/tableDisplayIds";
import { getDepartmentOptions } from "../../services/departmentsApi";
import { getRoleOptions } from "../../services/rolesApi";
import {
  createUser,
  deleteUser,
  getUsers,
  updateUser,
  updateUserStatus,
} from "../../services/usersApi";

const ALL_ROLES_FILTER = "All";
const ACTIVE_STATUS = "Active";
const DISABLED_STATUS = "Disabled";

const findUserById = (id) => users.value.find((item) => item.id === id);

const toUserRequest = (payload) => ({
  name: payload.name,
  nrcNumber: payload.nrcNumber,
  email: payload.email,
  role: payload.role,
  status: payload.status || ACTIVE_STATUS,
  phone: payload.phone,
  department: payload.department,
  title: payload.title,
  location: payload.location,
  manager: payload.manager,
  licenseNumber: payload.licenseNumber || null,
  licenseClass: payload.licenseClass || null,
  licenseExpiry: payload.licenseExpiry || null,
  emergencyContactName: payload.emergencyContactName,
  emergencyContactRelation: payload.emergencyContactRelation,
  emergencyContactPhone: payload.emergencyContactPhone,
  address: payload.address,
  twoFactorEnabled: Boolean(payload.twoFactorEnabled),
  notes: payload.notes || null,
  avatarFile: payload.avatarFile || null,
  nrcFrontFile: payload.nrcFrontFile || null,
  nrcBackFile: payload.nrcBackFile || null,
});

const userRoles = ref([]);
const departmentOptions = ref([]);
const roleFilter = ref(ALL_ROLES_FILTER);
const userStats = ref({ total: 0, active: 0, drivers: 0, admins: 0 });
const dialogOpen = ref(false);
const editOpen = ref(false);
const selectedUser = ref(null);
const avatarOpen = ref(false);
const avatarUrl = ref("");
const avatarName = ref("");
const { pageMessage, clearPageMessage, showPageMessage } = usePageMessage();
const {
  confirmOpen,
  confirmTitle,
  confirmMessage,
  confirmButton,
  confirmTone,
  openConfirm,
  runConfirm,
} = useConfirmDialog();
const {
  items: users,
  total: totalUsers,
  searchQuery,
  tableOptions,
  loading: loadingUsers,
  loadItems: loadUsers,
  handleTableOptions,
} = useListPage({
  fetchPage: ({ page, pageSize, search, sortBy, sortOrder }) =>
    getUsers({
      page,
      pageSize,
      search,
      role: roleFilter.value === ALL_ROLES_FILTER ? "" : roleFilter.value,
      sortBy,
      sortOrder,
    }),
  clearPageMessage,
  showPageMessage,
  errorTitle: "Could not load users",
  watchSources: [roleFilter],
  initialTableOptions: {
    sortBy: "name",
    sortOrder: "asc",
  },
  onLoaded: (result) => {
    userStats.value = result?.stats || {
      total: totalUsers.value,
      active: 0,
      drivers: 0,
      admins: 0,
    };
  },
  autoLoad: false,
});

const activeCount = computed(() => userStats.value.active);
const driverCount = computed(() => userStats.value.drivers);
const adminCount = computed(() => userStats.value.admins);
const tableUsers = computed(() =>
  attachDisplayIds(
    users.value,
    tableOptions.value.page,
    tableOptions.value.itemsPerPage,
    false,
    () => "USR",
    {
      total: totalUsers.value,
      sortBy: tableOptions.value.sortBy,
      sortOrder: tableOptions.value.sortOrder,
    },
  ),
);

const loadUserRoles = async () => {
  try {
    userRoles.value = await getRoleOptions();
  } catch (error) {
    console.error("[users] failed to load role options", error);
    showPageMessage({ tone: "error", title: "Could not load roles", message: error.message });
  }
};

const loadUserCodeOptions = async () => {
  try {
    departmentOptions.value = await getDepartmentOptions();
  } catch (error) {
    console.error("[users] failed to load code setup options", error);
    showPageMessage({ tone: "error", title: "Could not load code setup", message: error.message });
  }
};

const handleAdd = async (payload) => {
  clearPageMessage();

  try {
    const savedUser = await createUser(toUserRequest(payload));
    await loadUsers();
    dialogOpen.value = false;
    showPageMessage({
      tone: "success",
      title: "User added",
      message: `${savedUser.name} has been added successfully.`,
    });
  } catch (error) {
    showPageMessage({ tone: "error", title: "User was not added", message: error.message });
  }
};

const handleEdit = (id) => {
  const user = findUserById(id);
  if (!user) return;
  selectedUser.value = { ...user };
  editOpen.value = true;
};

const openAvatar = (user) => {
  if (!user?.avatar) return;
  avatarUrl.value = user.avatar;
  avatarName.value = user.name;
  avatarOpen.value = true;
};

const handleToggle = (id) => {
  const user = findUserById(id);
  if (!user) return;
  const nextStatus = user.status === ACTIVE_STATUS ? DISABLED_STATUS : ACTIVE_STATUS;

  openConfirm({
    title: `${nextStatus} User?`,
    message: `This will mark ${user.name} as ${nextStatus.toLowerCase()}.`,
    confirmText: nextStatus,
    tone: "warning",
    action: async () => {
      clearPageMessage();

      try {
        const savedUser = await updateUserStatus(id, nextStatus);
        users.value = users.value.map((item) =>
          item.id === id ? savedUser : item,
        );
        showPageMessage({
          tone: nextStatus === ACTIVE_STATUS ? "success" : "warning",
          title: nextStatus === ACTIVE_STATUS ? "User enabled" : "User disabled",
          message: `${savedUser.name} is now ${nextStatus.toLowerCase()}.`,
        });
      } catch (error) {
        showPageMessage({ tone: "error", title: "Status was not updated", message: error.message });
      }
    },
  });
};

const handleDelete = (id) => {
  const user = findUserById(id);
  if (!user) return;

  openConfirm({
    title: "Delete User?",
    message: `This will permanently remove ${user.name}.`,
    confirmText: "Delete",
    tone: "danger",
    action: async () => {
      clearPageMessage();

      try {
        await deleteUser(id);
        await loadUsers();
        showPageMessage({
          tone: "warning",
          title: "User deleted",
          message: `${user.name} has been removed.`,
        });
      } catch (error) {
        showPageMessage({ tone: "error", title: "User was not deleted", message: error.message });
      }
    },
  });
};

const handleUpdate = async (payload) => {
  clearPageMessage();

  try {
    const savedUser = await updateUser(payload.id, toUserRequest(payload));
    users.value = users.value.map((item) =>
      item.id === savedUser.id ? savedUser : item,
    );
    editOpen.value = false;
    showPageMessage({
      tone: "success",
      title: "User updated",
      message: `${savedUser.name} has been updated successfully.`,
    });
  } catch (error) {
    showPageMessage({ tone: "error", title: "User was not updated", message: error.message });
  }
};

onMounted(async () => {
  await Promise.all([loadUserRoles(), loadUserCodeOptions(), loadUsers()]);
});
</script>

<style scoped src="./users_styles/UserManagementContent.css"></style>
