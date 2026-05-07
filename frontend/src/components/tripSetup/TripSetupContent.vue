<template>
  <div class="role-page">
    <div class="page-header">
      <div>
        <h1 class="section-title">{{ title }}</h1>
        <p class="section-subtitle">{{ subtitle }}</p>
      </div>
    </div>

    <div class="stats-grid">
      <div class="stat-card">
        <p>Total {{ pluralLabel }}</p>
        <h3>{{ total }}</h3>
      </div>
      <div class="stat-card">
        <p>Active {{ pluralLabel }}</p>
        <h3 class="text-info">{{ activeItems }}</h3>
      </div>
      <div class="stat-card">
        <p>Disabled {{ pluralLabel }}</p>
        <h3 class="text-success">{{ disabledItems }}</h3>
      </div>
      <div class="stat-card">
        <p>Recently Updated</p>
        <h3 class="text-purple">{{ recentlyUpdatedItems }}</h3>
      </div>
    </div>

    <div class="card-surface toolbar">
      <div class="toolbar-row">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input
            v-model="searchQuery"
            type="text"
            :placeholder="`Search ${label.toLowerCase()} name, code, or description...`"
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
          v-if="canCreateSetup"
          class="primary-button"
          type="button"
          @click="openAdd"
        >
          <v-icon icon="mdi-plus" size="18" />
          Add {{ label }}
        </button>
      </div>

      <div class="toolbar-count text-muted">
        {{
          loading
            ? `Loading ${label.toLowerCase()}...`
            : `Showing ${items.length} of ${total} ${label.toLowerCase()} records`
        }}
      </div>
    </div>

    <div v-if="pageMessage.message" class="message-section">
      <PageMessage
        :tone="pageMessage.tone"
        :title="pageMessage.title"
        :message="pageMessage.message"
        @close="clearPageMessage"
      />
    </div>

    <div class="setup-table-section">
      <FuelTypeSetupTable
        :items="tableItems"
        :total="total"
        :loading="loading"
        :page="tableOptions.page"
        :items-per-page="tableOptions.itemsPerPage"
        :sort-by="tableOptions.sortBy"
        :sort-order="tableOptions.sortOrder"
        :label="label"
        :can-edit="canEditSetup"
        :can-delete="canDeleteSetup"
        @update:options="handleTableOptions"
        @edit="openEdit"
        @remove="handleDelete"
      />
    </div>

    <FuelTypeSetupDialog
      :open="dialogOpen"
      :mode="dialogMode"
      :item="selectedItem"
      :label="label"
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
import FuelTypeSetupDialog from "../fuelTypes/FuelTypeSetupDialog.vue";
import FuelTypeSetupTable from "../fuelTypes/FuelTypeSetupTable.vue";
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

const props = defineProps({
  title: { type: String, required: true },
  subtitle: { type: String, required: true },
  label: { type: String, required: true },
  codePrefix: { type: String, required: true },
  moduleKey: { type: String, required: true },
  api: { type: Object, required: true },
});

const dialogOpen = ref(false);
const dialogMode = ref("add");
const selectedItem = ref(null);
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
  items,
  total,
  searchQuery,
  tableOptions,
  loading,
  loadItems,
  handleTableOptions,
} = useListPage({
  fetchPage: ({ page, pageSize, search, sortBy, sortOrder }) =>
    props.api.list({ page, pageSize, search, sortBy, sortOrder }),
  clearPageMessage,
  showPageMessage,
  errorTitle: `Could not load ${props.label.toLowerCase()}`,
});
const {
  activeCount: activeItems,
  disabledCount: disabledItems,
  recentlyUpdatedCount: recentlyUpdatedItems,
} = useReferenceMetrics(items);
const pluralLabel = computed(() => `${props.label}s`);
const canCreateSetup = computed(() => canCreateModule(props.moduleKey));
const canEditSetup = computed(() => canEditModule(props.moduleKey));
const canDeleteSetup = computed(() => canDeleteModule(props.moduleKey));

const tableItems = computed(() =>
  attachDisplayIds(
    items.value,
    tableOptions.value.page,
    tableOptions.value.itemsPerPage,
    false,
    () => props.codePrefix,
    {
      total: total.value,
      sortBy: tableOptions.value.sortBy,
      sortOrder: tableOptions.value.sortOrder,
    },
  ),
);

const openAdd = () => {
  selectedItem.value = null;
  dialogMode.value = "add";
  dialogOpen.value = true;
};

const openEdit = (item) => {
  selectedItem.value = item;
  dialogMode.value = "edit";
  dialogOpen.value = true;
};

const handleSave = async (payload) => {
  try {
    if (dialogMode.value === "edit") {
      await props.api.update(payload.id, payload);
      showPageMessage({
        tone: "success",
        title: `${props.label} updated`,
        message: `${payload.name} was updated.`,
      });
    } else {
      await props.api.create(payload);
      showPageMessage({
        tone: "success",
        title: `${props.label} created`,
        message: `${payload.name} was created.`,
      });
    }
    dialogOpen.value = false;
    await loadItems();
  } catch (error) {
    showPageMessage({
      tone: "danger",
      title: `Could not save ${props.label.toLowerCase()}`,
      message: error.message,
    });
  }
};

const handleDelete = (item) => {
  openConfirm({
    title: `Delete ${props.label}?`,
    message: `This will permanently remove ${item.name}.`,
    confirmText: "Delete",
    tone: "danger",
    action: async () => {
      await props.api.delete(item.id);
      showPageMessage({
        tone: "success",
        title: `${props.label} deleted`,
        message: `${item.name} was removed.`,
      });
      await loadItems();
    },
  });
};
</script>

<style scoped src="../roles/roles_styles/RoleManagementContent.css"></style>
<style scoped src="./trip_setup_styles/TripSetupContent.css"></style>
