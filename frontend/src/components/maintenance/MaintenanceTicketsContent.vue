<template>
  <div class="role-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Maintenance Tickets</h1>
        <p class="section-subtitle">Track vehicle issues and repair progress with the same admin workflow.</p>
      </div>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Tickets</p>
        <h3>{{ totalTickets }}</h3>
      </div>
      <div class="stat-card">
        <p>Pending</p>
        <h3 class="text-purple">{{ pendingCount }}</h3>
      </div>
      <div class="stat-card">
        <p>Repairing</p>
        <h3 class="text-info">{{ repairingCount }}</h3>
      </div>
      <div class="stat-card">
        <p>Completed</p>
        <h3 class="text-success">{{ completedCount }}</h3>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search ticket ID, vehicle, issue, or mechanic..."
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
          <div v-if="showScopeFilter" class="toolbar-filter">
            <v-icon icon="mdi-account-switch-outline" />
            <select v-model="scopeFilter">
              <option value="mine">My Tickets</option>
              <option value="all">All Tickets</option>
            </select>
          </div>

          <div class="toolbar-filter">
            <v-icon icon="mdi-filter-variant" />
            <select v-model="statusFilter">
              <option value="All">All Status</option>
              <option v-for="status in statusOptions" :key="status" :value="status">
                {{ status }}
              </option>
            </select>
          </div>
        </div>

        <button v-if="canCreateTickets" class="primary-button" type="button" @click="openCreate">
          <v-icon icon="mdi-wrench" size="18" />
          Create Ticket
        </button>
      </div>

      <div class="toolbar-count text-muted">
        {{
          loadingTickets
            ? 'Loading tickets...'
            : `Showing ${tickets.length} of ${totalTickets} tickets`
        }}
      </div>
    </div>

    <PageMessage
      :tone="pageMessage.tone"
      :title="pageMessage.title"
      :message="pageMessage.message"
      @close="clearPageMessage"
    />

    <MaintenanceTicketsTable
      :items="tickets"
      :total="totalTickets"
      :loading="loadingTickets"
      :page="tableOptions.page"
      :items-per-page="tableOptions.itemsPerPage"
      :sort-by="tableOptions.sortBy"
      :sort-order="tableOptions.sortOrder"
      @update:options="handleTableOptions"
      @edit="openEdit"
      @advance-status="advanceStatus"
      @remove="deleteTicket"
      :can-edit="canEditTickets"
      :can-delete="canDeleteTickets"
    />

    <MaintenanceTicketDialog
      :open="dialogOpen"
      :mode="dialogMode"
      :ticket="selectedTicket"
      :mechanics="mechanicOptions"
      :statuses="statusOptions"
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
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from "vue";
import ConfirmDialog from "../common/ConfirmDialog.vue";
import PageMessage from "../common/PageMessage.vue";
import { useConfirmDialog } from "../../composables/useConfirmDialog";
import { useListPage } from "../../composables/useListPage";
import { usePageMessage } from "../../composables/usePageMessage";
import { canCreateModule, canDeleteModule, canEditModule, getCurrentUser } from "../../utils/authSession";
import {
  createMaintenanceTicket,
  deleteMaintenanceTicket,
  getMaintenanceTickets,
  updateMaintenanceTicket,
  updateMaintenanceTicketStatus,
} from "../../services/maintenanceTicketsApi";
import { getUserOptions } from "../../services/usersApi";
import { statusesApi } from "../../services/tripSetupApi";
import MaintenanceTicketDialog from "./MaintenanceTicketDialog.vue";
import MaintenanceTicketsTable from "./MaintenanceTicketsTable.vue";

const ALL_STATUS_FILTER = "All";

const statusFilter = ref(ALL_STATUS_FILTER);
const currentUser = computed(() => getCurrentUser());
const currentRole = computed(() => String(currentUser.value?.roleId || currentUser.value?.role || "").toLowerCase());
const showScopeFilter = computed(() => currentRole.value === "mechanic");
const scopeFilter = ref(showScopeFilter.value ? "mine" : "all");
const dialogOpen = ref(false);
const dialogMode = ref("create");
const selectedTicket = ref(null);
const mechanicOptions = ref([]);
const statusOptions = ref([]);
const canCreateTickets = computed(() => canCreateModule("maintenance-tickets"));
const canEditTickets = computed(() => canEditModule("maintenance-tickets"));
const canDeleteTickets = computed(() => canDeleteModule("maintenance-tickets"));
const ticketStats = ref({
  total: 0,
  pending: 0,
  repairing: 0,
  completed: 0,
});

const { pageMessage, clearPageMessage, showPageMessage } = usePageMessage();
const {
  confirmOpen,
  confirmTitle,
  confirmMessage,
  confirmButton,
  confirmTone,
  openConfirm,
  runConfirm
} = useConfirmDialog();

const {
  items: tickets,
  total: totalTickets,
  searchQuery,
  tableOptions,
  loading: loadingTickets,
  loadItems: loadTickets,
  handleTableOptions,
} = useListPage({
  fetchPage: ({ page, pageSize, search, sortBy, sortOrder }) =>
    getMaintenanceTickets({
      page,
      pageSize,
      search,
      status: statusFilter.value === ALL_STATUS_FILTER ? "" : statusFilter.value,
      scope: scopeFilter.value,
      sortBy,
      sortOrder,
    }),
  clearPageMessage,
  showPageMessage,
  errorTitle: "Could not load tickets",
  watchSources: [statusFilter, scopeFilter],
  initialTableOptions: { sortBy: "id", sortOrder: "asc" },
  onLoaded: (result) => {
    ticketStats.value = result?.stats || {
      total: 0,
      pending: 0,
      repairing: 0,
      completed: 0,
    };
  },
  autoLoad: false,
});

const pendingCount = computed(() => ticketStats.value.pending || 0);
const repairingCount = computed(() => ticketStats.value.repairing || 0);
const completedCount = computed(() => ticketStats.value.completed || 0);

const loadMechanicOptions = async () => {
  try {
    mechanicOptions.value = await getUserOptions({ role: "Mechanic" });
  } catch (error) {
    showPageMessage({
      tone: "error",
      title: "Could not load mechanics",
      message: error.message,
    });
  }
};

const loadStatusOptions = async () => {
  try {
    statusOptions.value = await statusesApi.options();
  } catch (error) {
    showPageMessage({
      tone: "error",
      title: "Could not load statuses",
      message: error.message,
    });
  }
};

const openCreate = () => {
  dialogMode.value = "create";
  selectedTicket.value = {
    vehicle: "",
    vehicleId: "",
    issue: "",
    details: "",
    reportedDate: "",
    mechanic: "",
    status: statusOptions.value[0] || "",
  };
  dialogOpen.value = true;
};

const openEdit = (ticket) => {
  dialogMode.value = "edit";
  selectedTicket.value = { ...ticket };
  dialogOpen.value = true;
};

const toTicketRequest = (payload) => ({
  vehicle: payload.vehicle,
  vehicleId: payload.vehicleId,
  issue: payload.issue,
  details: payload.details,
  reportedDate: payload.reportedDate,
  mechanic: payload.mechanic,
  status: payload.status,
});

const handleSave = async (payload) => {
  clearPageMessage();
  const isCreate = dialogMode.value === "create";

  try {
    const savedTicket = isCreate
      ? await createMaintenanceTicket(toTicketRequest(payload))
      : await updateMaintenanceTicket(payload.id, toTicketRequest(payload));

    await loadTickets();
    dialogOpen.value = false;
    showPageMessage({
      tone: "success",
      title: isCreate ? "Ticket created" : "Ticket updated",
      message: `${savedTicket.id} has been ${isCreate ? "created" : "updated"} successfully.`,
    });
  } catch (error) {
    showPageMessage({
      tone: "error",
      title: "Ticket was not saved",
      message: error.message,
    });
  }
};

const advanceStatus = (ticket) => {
  const statuses = statusOptions.value.length ? statusOptions.value : [ticket.status];
  const currentIndex = statuses.indexOf(ticket.status);
  const nextStatus = statuses[(currentIndex + 1) % statuses.length] || ticket.status;

  openConfirm({
    title: "Update Status?",
    message: `Move ${ticket.id} to ${nextStatus.toLowerCase()} status?`,
    confirmText: nextStatus,
    tone: "warning",
    action: async () => {
      clearPageMessage();

      try {
        await updateMaintenanceTicketStatus(ticket.id, nextStatus);
        await loadTickets();
        showPageMessage({
          tone: "success",
          title: "Status updated",
          message: `${ticket.id} is now ${nextStatus.toLowerCase()}.`,
        });
      } catch (error) {
        showPageMessage({
          tone: "error",
          title: "Status was not updated",
          message: error.message,
        });
      }
    },
  });
};

const deleteTicket = (ticket) => {
  openConfirm({
    title: "Delete Ticket?",
    message: `This will permanently remove ${ticket.id}.`,
    confirmText: "Delete",
    tone: "danger",
    action: async () => {
      clearPageMessage();

      try {
        await deleteMaintenanceTicket(ticket.id);
        await loadTickets();
        showPageMessage({
          tone: "warning",
          title: "Ticket deleted",
          message: `${ticket.id} has been removed.`,
        });
      } catch (error) {
        showPageMessage({
          tone: "error",
          title: "Ticket was not deleted",
          message: error.message,
        });
      }
    },
  });
};

onMounted(async () => {
  await Promise.all([loadMechanicOptions(), loadStatusOptions(), loadTickets()]);
});
</script>

<style scoped src="../roles/roles_styles/RoleManagementContent.css"></style>
