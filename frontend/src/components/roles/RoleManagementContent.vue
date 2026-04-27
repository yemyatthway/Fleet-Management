<template>
  <div class="role-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Role Management</h1>
        <p class="section-subtitle">
          View the four fixed system roles and their assigned users
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
        <p>Drivers</p>
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
      :roles="roles"
      :total="totalRoles"
      :loading="loadingRoles"
      :page="tableOptions.page"
      :items-per-page="tableOptions.itemsPerPage"
      :sort-by="tableOptions.sortBy"
      :sort-order="tableOptions.sortOrder"
      @update:options="handleTableOptions"
      @view="openMembers"
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
import RoleMembersDialog from "./RoleMembersDialog.vue";
import MemberAvatarDialog from "./MemberAvatarDialog.vue";
import PageMessage from "../common/PageMessage.vue";
import { useDebouncedRef } from "../../composables/useDebouncedRef";
import { useListPage } from "../../composables/useListPage";
import { usePageMessage } from "../../composables/usePageMessage";
import {
  getRoleMembers,
  getRoleOptions,
  getRoles,
} from "../../services/rolesApi";

const ALL_ROLES_FILTER = "All";

const memberHeaders = [
  { title: "Name", key: "name", sortable: false },
  { title: "Email", key: "email", sortable: false },
  { title: "Phone", key: "phone", sortable: false },
  { title: "Status", key: "status", sortable: false },
  { title: "Joined", key: "joinDate", sortable: false },
];

const SEARCHABLE_MEMBER_FIELDS = ["name", "email", "phone"];

const normalizeText = (value) => String(value ?? "").toLowerCase();

const matchesSearch = (item, fields, query) =>
  !query || fields.some((field) => normalizeText(item[field]).includes(query));

const roleMembers = ref([]);
const roleOptions = ref([]);
const activeTab = ref(ALL_ROLES_FILTER);
const loadingMembers = ref(false);
const selectedRole = ref(null);
const membersOpen = ref(false);
const memberSearch = ref("");
const memberAvatarOpen = ref(false);
const memberAvatarUrl = ref("");
const memberAvatarName = ref("");
const { pageMessage, clearPageMessage, showPageMessage } = usePageMessage();

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
