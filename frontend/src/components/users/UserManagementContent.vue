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
        {{ loadingUsers ? 'Loading users...' : `Showing ${filteredUsers.length} of ${users.length} users` }}
      </div>
    </div>

    <div v-if="pageError" class="page-error" role="alert">
      <span>{{ pageError }}</span>
      <button class="page-error-close" type="button" aria-label="Close error message" @click="pageError = ''">
        <v-icon icon="mdi-close" size="18" />
      </button>
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
import { roleNames } from "../../data/roles";
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
const SEARCHABLE_USER_FIELDS = [
  "name",
  "email",
  "nrcNumber",
  "employeeId",
  "phone",
  "department",
  "title",
  "location",
];

const normalizeText = (value) => String(value ?? "").toLowerCase();

const matchesSearch = (item, fields, query) =>
  !query || fields.some((field) => normalizeText(item[field]).includes(query));

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

const countUsers = (userList) =>
  userList.reduce(
    (stats, user) => {
      if (user.status === ACTIVE_STATUS) stats.active += 1;
      if (user.role === "Driver") stats.drivers += 1;
      if (user.role === "Admin") stats.admins += 1;
      return stats;
    },
    { active: 0, drivers: 0, admins: 0 },
  );

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
const roleFilter = ref(ALL_ROLES_FILTER);
const pageError = ref("");
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

const userStats = computed(() => countUsers(users.value));

const filteredUsers = computed(() => {
  const query = normalizeText(debouncedQuery.value);

  return users.value.filter((user) => {
    const matchesRole =
      roleFilter.value === ALL_ROLES_FILTER || user.role === roleFilter.value;

    return matchesRole && matchesSearch(user, SEARCHABLE_USER_FIELDS, query);
  });
});

const activeCount = computed(() => userStats.value.active);
const driverCount = computed(() => userStats.value.drivers);
const adminCount = computed(() => userStats.value.admins);

const loadUsers = async () => {
  loadingUsers.value = true;
  pageError.value = "";

  try {
    users.value = await getUsers();
  } catch (error) {
    pageError.value = error.message;
  } finally {
    loadingUsers.value = false;
  }
};

const handleAdd = async (payload) => {
  pageError.value = "";

  try {
    const savedUser = await createUser(toUserRequest(payload));
    users.value = [savedUser, ...users.value];
    dialogOpen.value = false;
  } catch (error) {
    pageError.value = error.message;
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
      pageError.value = "";

      try {
        const savedUser = await updateUserStatus(id, nextStatus);
        users.value = users.value.map((item) =>
          item.id === id ? savedUser : item,
        );
      } catch (error) {
        pageError.value = error.message;
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
      pageError.value = "";

      try {
        await deleteUser(id);
        users.value = users.value.filter((item) => item.id !== id);
      } catch (error) {
        pageError.value = error.message;
      }
    },
  });
};

const handleUpdate = async (payload) => {
  pageError.value = "";

  try {
    const savedUser = await updateUser(payload.id, toUserRequest(payload));
    users.value = users.value.map((item) =>
      item.id === savedUser.id ? savedUser : item,
    );
    editOpen.value = false;
  } catch (error) {
    pageError.value = error.message;
  }
};

onMounted(loadUsers);
</script>

<style scoped src="./users_styles/UserManagementContent.css"></style>
