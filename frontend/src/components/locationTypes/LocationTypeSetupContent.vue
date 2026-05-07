<template>
  <div class="role-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">Location Type Setup</h1>
        <p class="section-subtitle">
          Manage location type master data for warehouses, depots, hubs, and
          yards.
        </p>
      </div>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total Location Types</p>
        <h3>{{ totalLocationTypes }}</h3>
      </div>
      <div class="stat-card">
        <p>Active Location Types</p>
        <h3 class="text-info">{{ activeLocationTypes }}</h3>
      </div>
      <div class="stat-card">
        <p>Disabled Location Types</p>
        <h3 class="text-success">{{ disabledLocationTypes }}</h3>
      </div>
      <div class="stat-card">
        <p>Recently Updated</p>
        <h3 class="text-purple">{{ recentlyUpdatedLocationTypes }}</h3>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Search location type name, code, or description..."
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

        <button
          v-if="canCreateLocationTypes"
          class="primary-button"
          type="button"
          @click="openAdd"
        >
          <v-icon icon="mdi-plus" size="18" />
          Add Location Type
        </button>
      </div>

      <div class="toolbar-count text-muted">
        {{
          loadingLocationTypes
            ? "Loading location types..."
            : `Showing ${locationTypes.length} of ${totalLocationTypes} location types`
        }}
      </div>
    </div>

    <PageMessage
      :tone="pageMessage.tone"
      :title="pageMessage.title"
      :message="pageMessage.message"
      @close="clearPageMessage"
    />

    <LocationTypeSetupTable
      :items="tableLocationTypes"
      :total="totalLocationTypes"
      :loading="loadingLocationTypes"
      :page="tableOptions.page"
      :items-per-page="tableOptions.itemsPerPage"
      :sort-by="tableOptions.sortBy"
      :sort-order="tableOptions.sortOrder"
      :can-edit="canEditLocationTypes"
      :can-delete="canDeleteLocationTypes"
      @update:options="handleTableOptions"
      @edit="openEdit"
      @remove="handleDelete"
    />

    <LocationTypeSetupDialog
      :open="dialogOpen"
      :mode="dialogMode"
      :item="selectedLocationType"
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
import { computed, ref } from "vue";
import ConfirmDialog from "../common/ConfirmDialog.vue";
import PageMessage from "../common/PageMessage.vue";
import { useConfirmDialog } from "../../composables/useConfirmDialog";
import { useListPage } from "../../composables/useListPage";
import { usePageMessage } from "../../composables/usePageMessage";
import { useReferenceMetrics } from "../../composables/useReferenceMetrics";
import { attachDisplayIds } from "../../utils/tableDisplayIds";
import {
  canCreateModule,
  canDeleteModule,
  canEditModule,
} from "../../utils/authSession";
import {
  createLocationTypeCodeOption,
  deleteLocationTypeCodeOption,
  getLocationTypeCodeOptions,
  updateLocationTypeCodeOption,
} from "../../services/locationTypesApi";
import LocationTypeSetupDialog from "./LocationTypeSetupDialog.vue";
import LocationTypeSetupTable from "./LocationTypeSetupTable.vue";

const dialogOpen = ref(false);
const dialogMode = ref("add");
const selectedLocationType = ref(null);
const canCreateLocationTypes = computed(() =>
  canCreateModule("location-type-setup"),
);
const canEditLocationTypes = computed(() =>
  canEditModule("location-type-setup"),
);
const canDeleteLocationTypes = computed(() =>
  canDeleteModule("location-type-setup"),
);
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
  items: locationTypes,
  total: totalLocationTypes,
  searchQuery,
  tableOptions,
  loading: loadingLocationTypes,
  loadItems: loadLocationTypes,
  handleTableOptions,
} = useListPage({
  fetchPage: ({ page, pageSize, search, sortBy, sortOrder }) =>
    getLocationTypeCodeOptions({ page, pageSize, search, sortBy, sortOrder }),
  clearPageMessage,
  showPageMessage,
  errorTitle: "Could not load location types",
});
const {
  activeCount: activeLocationTypes,
  disabledCount: disabledLocationTypes,
  recentlyUpdatedCount: recentlyUpdatedLocationTypes,
} = useReferenceMetrics(locationTypes);
const tableLocationTypes = computed(() =>
  attachDisplayIds(
    locationTypes.value,
    tableOptions.value.page,
    tableOptions.value.itemsPerPage,
    false,
    () => "LTP",
    {
      total: totalLocationTypes.value,
      sortBy: tableOptions.value.sortBy,
      sortOrder: tableOptions.value.sortOrder,
    },
  ),
);

const openAdd = () => {
  dialogMode.value = "add";
  selectedLocationType.value = { status: "Active" };
  dialogOpen.value = true;
};

const openEdit = (item) => {
  dialogMode.value = "edit";
  selectedLocationType.value = { ...item };
  dialogOpen.value = true;
};

const resetTableOrder = () => {
  tableOptions.value = {
    ...tableOptions.value,
    page: 1,
    sortBy: "id",
    sortOrder: "asc",
  };
};

const handleSave = async (payload) => {
  clearPageMessage();
  const isEdit = dialogMode.value === "edit";

  try {
    const savedLocationType = isEdit
      ? await updateLocationTypeCodeOption(payload.id, payload)
      : await createLocationTypeCodeOption(payload);

    resetTableOrder();
    await loadLocationTypes();

    dialogOpen.value = false;
    showPageMessage({
      tone: "success",
      title: isEdit ? "Location type updated" : "Location type created",
      message: `${savedLocationType.name} has been ${isEdit ? "updated" : "created"} successfully.`,
    });
  } catch (error) {
    showPageMessage({
      tone: "error",
      title: "Location type was not saved",
      message: error.message,
    });
  }
};

const handleDelete = (item) => {
  openConfirm({
    title: "Delete Location Type?",
    message: `This will permanently remove ${item.name}.`,
    confirmText: "Delete",
    tone: "danger",
    action: async () => {
      clearPageMessage();

      try {
        await deleteLocationTypeCodeOption(item.id);
        await loadLocationTypes();
        showPageMessage({
          tone: "warning",
          title: "Location type deleted",
          message: `${item.name} has been removed.`,
        });
      } catch (error) {
        showPageMessage({
          tone: "error",
          title: "Location type was not deleted",
          message: error.message,
        });
      }
    },
  });
};
</script>

<style scoped src="../roles/roles_styles/RoleManagementContent.css"></style>
