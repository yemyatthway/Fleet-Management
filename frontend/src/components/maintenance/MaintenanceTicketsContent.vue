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

        <div class="toolbar-filter">
          <v-icon icon="mdi-filter-variant" />
          <select v-model="statusFilter">
            <option value="All">All Status</option>
            <option value="Pending">Pending</option>
            <option value="Repairing">Repairing</option>
            <option value="Completed">Completed</option>
          </select>
        </div>

        <button class="primary-button" type="button" @click="openCreate">
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
    />

    <MaintenanceTicketDialog
      :open="dialogOpen"
      :mode="dialogMode"
      :ticket="selectedTicket"
      :mechanics="mechanicOptions"
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
import {
  createMaintenanceTicket,
  deleteMaintenanceTicket,
  getMaintenanceTickets,
  updateMaintenanceTicket,
  updateMaintenanceTicketStatus,
} from "../../services/maintenanceTicketsApi";
import { getUserOptions } from "../../services/usersApi";
import MaintenanceTicketDialog from "./MaintenanceTicketDialog.vue";
import MaintenanceTicketsTable from "./MaintenanceTicketsTable.vue";

const ALL_STATUS_FILTER = "All";

const statusFilter = ref(ALL_STATUS_FILTER);
const dialogOpen = ref(false);
const dialogMode = ref("create");
const selectedTicket = ref(null);
const mechanicOptions = ref([]);
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
      sortBy,
      sortOrder,
    }),
  clearPageMessage,
  showPageMessage,
  errorTitle: "Could not load tickets",
  watchSources: [statusFilter],
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

const openCreate = () => {
  dialogMode.value = "create";
  selectedTicket.value = {
    vehicle: "",
    vehicleId: "",
    issue: "",
    details: "",
    reportedDate: "",
    mechanic: "",
    status: "Pending",
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
  const nextStatus =
    ticket.status === "Pending"
      ? "Repairing"
      : ticket.status === "Repairing"
        ? "Completed"
        : "Pending";

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
  await Promise.all([loadMechanicOptions(), loadTickets()]);
});
</script>

<style scoped src="../roles/roles_styles/RoleManagementContent.css"></style>
