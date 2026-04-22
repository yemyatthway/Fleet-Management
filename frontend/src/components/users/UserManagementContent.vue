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
      :locations="locationOptions"
      @close="dialogOpen = false"
      @add="handleAdd"
    />

    <EditUserDialog
      :open="editOpen"
      :user="selectedUser"
      :roles="userRoles"
      :departments="departmentOptions"
      :locations="locationOptions"
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
import { computed, onBeforeUnmount, onMounted, ref, watch } from "vue";
import UserTable from "./UserTable.vue";
import AddUserDialog from "./AddUserDialog.vue";
import EditUserDialog from "./EditUserDialog.vue";
import UserAvatarDialog from "./UserAvatarDialog.vue";
import ConfirmDialog from "../common/ConfirmDialog.vue";
import PageMessage from "../common/PageMessage.vue";
import { attachDisplayIds } from "../../utils/tableDisplayIds";
import { getRoleOptions } from "../../services/rolesApi";
import { getDepartmentOptions, getLocationOptions } from "../../services/userCodeOptionsApi";
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
const SEARCH_DELAY_MS = 350;
const PAGE_MESSAGE_DURATION_MS = 5000;
const useDebouncedRef = (source, delay = SEARCH_DELAY_MS) => {
  const debounced = ref(source.value);
  let timerId = null;

  const clearTimer = () => {
    if (timerId) clearTimeout(timerId);
  };

  watch(
    source,
    (value) => {
      clearTimer();
      timerId = setTimeout(() => {
        debounced.value = value;
      }, delay);
    },
    { immediate: true },
  );

  onBeforeUnmount(clearTimer);

  return debounced;
};

const findUserById = (id) => users.value.find((item) => item.id === id);

const toUserRequest = (payload) => ({
  name: payload.name,
  employeeId: payload.employeeId,
  nrcNumber: payload.nrcNumber,
  email: payload.email,
  role: payload.role,
  status: payload.status || ACTIVE_STATUS,
  phone: payload.phone,
  avatar: payload.avatar,
  nrcFront: payload.nrcFront,
  nrcBack: payload.nrcBack,
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
});

const users = ref([]);
const searchQuery = ref("");
const debouncedQuery = useDebouncedRef(searchQuery);
const userRoles = ref([]);
const departmentOptions = ref([]);
const locationOptions = ref([]);
const roleFilter = ref(ALL_ROLES_FILTER);
const totalUsers = ref(0);
const userStats = ref({ total: 0, active: 0, drivers: 0, admins: 0 });
const tableOptions = ref({ page: 1, itemsPerPage: 10, sortBy: "name", sortOrder: "asc" });
const pageMessage = ref({ tone: "info", title: "", message: "" });
const loadingUsers = ref(false);
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
let pageMessageTimerId = null;

const activeCount = computed(() => userStats.value.active);
const driverCount = computed(() => userStats.value.drivers);
const adminCount = computed(() => userStats.value.admins);
const tableUsers = computed(() =>
  attachDisplayIds(users.value, tableOptions.value.page, tableOptions.value.itemsPerPage, () => "USR"),
);

const clearPageMessage = () => {
  if (pageMessageTimerId) {
    clearTimeout(pageMessageTimerId);
    pageMessageTimerId = null;
  }
  pageMessage.value = { tone: "info", title: "", message: "" };
};

const showPageMessage = ({ tone = "info", title = "", message }) => {
  if (pageMessageTimerId) clearTimeout(pageMessageTimerId);
  pageMessage.value = { tone, title, message };
  pageMessageTimerId = setTimeout(() => {
    pageMessageTimerId = null;
    clearPageMessage();
  }, PAGE_MESSAGE_DURATION_MS);
};

const loadUsers = async () => {
  loadingUsers.value = true;
  clearPageMessage();

  try {
    const result = await getUsers({
      page: tableOptions.value.page,
      pageSize: tableOptions.value.itemsPerPage,
      search: debouncedQuery.value,
      role: roleFilter.value === ALL_ROLES_FILTER ? '' : roleFilter.value,
      sortBy: tableOptions.value.sortBy,
      sortOrder: tableOptions.value.sortOrder,
    });
    users.value = result.items || [];
    totalUsers.value = result.total || 0;
    userStats.value = result.stats || { total: totalUsers.value, active: 0, drivers: 0, admins: 0 };
  } catch (error) {
    showPageMessage({ tone: "error", title: "Could not load users", message: error.message });
  } finally {
    loadingUsers.value = false;
  }
};

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
    const [departments, locations] = await Promise.all([getDepartmentOptions(), getLocationOptions()]);
    departmentOptions.value = departments;
    locationOptions.value = locations;
  } catch (error) {
    console.error("[users] failed to load code setup options", error);
    showPageMessage({ tone: "error", title: "Could not load code setup", message: error.message });
  }
};

const handleTableOptions = (options) => {
  const firstSort = options.sortBy?.[0];
  tableOptions.value = {
    page: options.page || 1,
    itemsPerPage: options.itemsPerPage || 10,
    sortBy: firstSort?.key || "name",
    sortOrder: firstSort?.order || "asc",
  };
  loadUsers();
};

watch([debouncedQuery, roleFilter], () => {
  tableOptions.value.page = 1;
  loadUsers();
});

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

const openConfirm = ({ title, message, confirmText, tone, action }) => {
  confirmTitle.value = title;
  confirmMessage.value = message;
  confirmButton.value = confirmText;
  confirmTone.value = tone;
  pendingAction.value = action;
  confirmOpen.value = true;
};

const runConfirm = async () => {
  await pendingAction.value();
  confirmOpen.value = false;
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

onBeforeUnmount(() => {
  if (pageMessageTimerId) clearTimeout(pageMessageTimerId);
});
</script>

<style scoped src="./users_styles/UserManagementContent.css"></style>
