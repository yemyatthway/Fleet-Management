<template>
  <div class="role-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Role Management</h1>
        <p class="section-subtitle">
          Define access levels and operational ownership
        </p>
      </div>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Roles</p>
        <h3>{{ totalRoles }}</h3>
      </div>
      <div class="stat-card">
        <p>Assigned Users</p>
        <h3 class="text-info">{{ totalMembers }}</h3>
      </div>
      <div class="stat-card">
        <p>Driver Roles</p>
        <h3 class="text-success">{{ driverMembers }}</h3>
      </div>
      <div class="stat-card">
        <p>Admins</p>
        <h3 class="text-purple">{{ adminMembers }}</h3>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search roles or descriptions..."
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

        <div class="toolbar-filter">
          <v-icon icon="mdi-filter-variant" />
          <select v-model="activeTab">
            <option value="All">All Roles</option>
            <option v-for="role in roleTabs" :key="role" :value="role">
              {{ role }}
            </option>
          </select>
        </div>

        <button class="primary-button" type="button" @click="openAdd">
          <v-icon icon="mdi-shield-plus" size="18" />
          Create Role
        </button>
      </div>

      <div class="toolbar-count text-muted">
        {{
          loadingRoles
            ? "Loading roles..."
            : `Showing ${roles.length} of ${totalRoles} roles`
        }}
      </div>
    </div>

    <PageMessage
      :tone="pageMessage.tone"
      :title="pageMessage.title"
      :message="pageMessage.message"
      @close="clearPageMessage"
    />

    <RoleTable
      :roles="tableRoles"
      :total="totalRoles"
      :loading="loadingRoles"
      :page="tableOptions.page"
      :items-per-page="tableOptions.itemsPerPage"
      :sort-by="tableOptions.sortBy"
      :sort-order="tableOptions.sortOrder"
      @update:options="handleTableOptions"
      @view="openMembers"
      @edit="openEdit"
      @remove="handleDelete"
    />

    <RoleDialog
      :open="dialogOpen"
      :mode="dialogMode"
      :role="selectedRole"
      @close="dialogOpen = false"
      @save="handleSave"
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

    <RoleMembersDialog
      :open="membersOpen"
      :role="selectedRole"
      :members="filteredMembers"
      :headers="memberHeaders"
      :loading="loadingMembers"
      :search="memberSearch"
      @update:open="membersOpen = $event"
      @update:search="memberSearch = $event"
      @view-avatar="openMemberAvatar"
    />

    <MemberAvatarDialog
      :open="memberAvatarOpen"
      :name="memberAvatarName"
      :url="memberAvatarUrl"
      @update:open="memberAvatarOpen = $event"
    />
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from "vue";
import RoleTable from "./RoleTable.vue";
import RoleDialog from "./RoleDialog.vue";
import RoleMembersDialog from "./RoleMembersDialog.vue";
import MemberAvatarDialog from "./MemberAvatarDialog.vue";
import ConfirmDialog from "../common/ConfirmDialog.vue";
import PageMessage from "../common/PageMessage.vue";
import { useConfirmDialog } from "../../composables/useConfirmDialog";
import { useDebouncedRef } from "../../composables/useDebouncedRef";
import { useListPage } from "../../composables/useListPage";
import { usePageMessage } from "../../composables/usePageMessage";
import {
  createRole,
  deleteRole,
  getRoleOptions,
  getRoleMembers,
  getRoles,
  updateRole,
} from "../../services/rolesApi";

const ALL_ROLES_FILTER = "All";

const memberHeaders = [
  { title: "Name", key: "name" },
  { title: "Email", key: "email" },
  { title: "Phone", key: "phone" },
  { title: "Status", key: "status" },
  { title: "Joined", key: "joinDate" },
];

const SEARCHABLE_MEMBER_FIELDS = ["name", "email", "phone"];

const normalizeText = (value) => String(value ?? "").toLowerCase();

const matchesSearch = (item, fields, query) =>
  !query || fields.some((field) => normalizeText(item[field]).includes(query));

const toRoleRequest = (payload) => ({
  name: payload.name,
  description: payload.description,
  status: payload.status || "Active",
});

const roleMembers = ref([]);
const roleOptions = ref([]);
const activeTab = ref(ALL_ROLES_FILTER);
const loadingMembers = ref(false);
const dialogOpen = ref(false);
const dialogMode = ref("add");
const selectedRole = ref(null);
const membersOpen = ref(false);
const memberSearch = ref("");
const memberAvatarOpen = ref(false);
const memberAvatarUrl = ref("");
const memberAvatarName = ref("");
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

const debouncedMemberQuery = useDebouncedRef(memberSearch);
const {
  items: roles,
  total: totalRoles,
  searchQuery,
  tableOptions,
  loading: loadingRoles,
  loadItems: loadRoles,
  handleTableOptions,
} = useListPage({
  fetchPage: ({ page, pageSize, search, sortBy, sortOrder }) =>
    getRoles({
      page,
      pageSize,
      search,
      role: activeTab.value === ALL_ROLES_FILTER ? "" : activeTab.value,
      sortBy,
      sortOrder,
    }),
  clearPageMessage,
  showPageMessage,
  errorTitle: "Could not load roles",
  watchSources: [activeTab],
  initialTableOptions: { sortBy: "code", sortOrder: "asc" },
  autoLoad: false,
});

const roleTabs = computed(() => roleOptions.value);

const totalMembers = computed(() =>
  roles.value.reduce((total, role) => total + (role.members || 0), 0),
);
const driverMembers = computed(
  () => roles.value.find((role) => role.name === "Driver")?.members || 0,
);
const adminMembers = computed(
  () => roles.value.find((role) => role.name === "Admin")?.members || 0,
);
const tableRoles = computed(() => roles.value);

const filteredMembers = computed(() => {
  const query = debouncedMemberQuery.value.toLowerCase();
  return roleMembers.value.filter((member) =>
    matchesSearch(member, SEARCHABLE_MEMBER_FIELDS, query),
  );
});

const loadRoleOptions = async () => {
  try {
    roleOptions.value = await getRoleOptions();
  } catch (error) {
    console.error("[roles] failed to load role options", error);
    showPageMessage({
      tone: "error",
      title: "Could not load roles",
      message: error.message,
    });
  }
};

const openAdd = () => {
  dialogMode.value = "add";
  selectedRole.value = null;
  dialogOpen.value = true;
};

const openEdit = (role) => {
  dialogMode.value = "edit";
  selectedRole.value = { ...role };
  dialogOpen.value = true;
};

const handleSave = async (payload) => {
  clearPageMessage();
  const isEdit = dialogMode.value === "edit";

  try {
    const savedRole = isEdit
      ? await updateRole(payload.id, toRoleRequest(payload))
      : await createRole(toRoleRequest(payload));

    if (isEdit) {
      roles.value = roles.value.map((role) =>
        role.id === savedRole.id ? savedRole : role,
      );
    } else {
      await loadRoles();
    }

    dialogOpen.value = false;
    showPageMessage({
      tone: "success",
      title: isEdit ? "Role updated" : "Role created",
      message: `${savedRole.name} has been ${isEdit ? "updated" : "created"} successfully.`,
    });
  } catch (error) {
    showPageMessage({
      tone: "error",
      title: "Role was not saved",
      message: error.message,
    });
  }
};

const openMembers = async (role) => {
  selectedRole.value = role;
  memberSearch.value = "";
  roleMembers.value = [];
  membersOpen.value = true;
  loadingMembers.value = true;
  clearPageMessage();

  try {
    roleMembers.value = await getRoleMembers(role.id);
  } catch (error) {
    showPageMessage({
      tone: "error",
      title: "Could not load members",
      message: error.message,
    });
  } finally {
    loadingMembers.value = false;
  }
};

const handleDelete = (role) => {
  openConfirm({
    title: "Delete Role?",
    message: `This will permanently remove ${role.name}.`,
    confirmText: "Delete",
    tone: "danger",
    action: async () => {
      clearPageMessage();

      try {
        await deleteRole(role.id);
        await loadRoles();
        showPageMessage({
          tone: "warning",
          title: "Role deleted",
          message: `${role.name} has been removed.`,
        });
      } catch (error) {
        showPageMessage({
          tone: "error",
          title: "Role was not deleted",
          message: error.message,
        });
      }
    },
  });
};

const openMemberAvatar = (member) => {
  if (!member?.avatar) return;
  memberAvatarUrl.value = member.avatar;
  memberAvatarName.value = member.name;
  memberAvatarOpen.value = true;
};

onMounted(async () => {
  await Promise.all([loadRoleOptions(), loadRoles()]);
});
</script>

<style scoped src="./roles_styles/RoleManagementContent.css"></style>
