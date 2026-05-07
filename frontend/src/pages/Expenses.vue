<template>
  <DashboardLayout>
    <main class="records-page">
      <header class="records-header">
        <div>
          <h1>Expenses</h1>
          <p>
            Track fuel, repair, toll, parking, insurance, tax, and trip costs.
          </p>
        </div>
        <button
          v-if="canCreate"
          class="primary-button"
          type="button"
          @click="startCreate"
        >
          <v-icon icon="mdi-plus" size="20" />
          Add Expense
        </button>
      </header>

      <PageMessage
        :tone="pageMessage.tone"
        :title="pageMessage.title"
        :message="pageMessage.message"
        @close="clearPageMessage"
      />

      <div v-if="pageError" class="page-error">{{ pageError }}</div>

      <section class="toolbar">
        <div class="search-box">
          <v-icon icon="mdi-magnify" size="22" />
          <input
            v-model="filters.search"
            placeholder="Search expense, vehicle, trip, driver..."
            @input="refreshRecords"
          />
        </div>
        <select v-model="filters.status" @change="refreshRecords">
          <option value="">All Status</option>
          <option v-for="status in statusOptions" :key="status" :value="status">
            {{ status }}
          </option>
        </select>
        <input
          v-model="filters.dateFrom"
          type="date"
          @change="refreshRecords"
        />
        <input v-model="filters.dateTo" type="date" @change="refreshRecords" />
      </section>

      <form v-if="showForm" class="record-form" @submit.prevent="saveRecord">
        <input v-model="form.expenseDate" type="date" required />
        <select v-model="form.expenseType" required>
          <option value="" disabled>Expense type</option>
          <option v-for="type in expenseTypeOptions" :key="type" :value="type">
            {{ type }}
          </option>
        </select>
        <input v-model="form.vehicleId" placeholder="Vehicle/ID" />
        <input v-model="form.tripNumber" placeholder="Trip number" />
        <input v-model="form.driverName" placeholder="Driver" />
        <input
          v-model.number="form.amount"
          min="0"
          step="0.01"
          type="number"
          placeholder="Amount"
          required
        />
        <select v-model="form.status" required>
          <option v-for="status in statusOptions" :key="status" :value="status">
            {{ status }}
          </option>
        </select>
        <input v-model="form.notes" placeholder="Notes" />
        <div class="form-actions">
          <button class="ghost-button" type="button" @click="cancelForm">
            Cancel
          </button>
          <button class="primary-button" type="submit">
            {{ editingId ? "Save Expense" : "Create Expense" }}
          </button>
        </div>
      </form>

      <section class="table-card">
        <div class="table-wrap">
          <v-data-table-server
            v-model:page="pagination.page"
            v-model:items-per-page="pagination.pageSize"
            class="table-base expenses-table"
            :headers="expenseHeaders"
            :items="tableRows"
            :items-length="totalRecords"
            :items-per-page-options="[10, 20, 30]"
            :loading="loading"
            :mobile-breakpoint="0"
            :mobile="false"
            fixed-header
            height="560"
            density="comfortable"
            @update:options="handleTableOptions"
          >
            <template #item.vehicleId="{ item }">
              <span class="text-muted">{{ item.vehicleId || "-" }}</span>
            </template>

            <template #item.tripNumber="{ item }">
              <span class="text-muted">{{ item.tripNumber || "-" }}</span>
            </template>

            <template #item.driverName="{ item }">
              <span>{{ item.driverName || "-" }}</span>
            </template>

            <template #item.amount="{ item }">
              <strong>{{ formatCurrency(item.amount) }}</strong>
            </template>

            <template #item.status="{ item }">
              <span class="role-badge" :class="statusClass(item.status)">{{
                item.status
              }}</span>
            </template>

            <template #item.actions="{ item }">
              <div class="inline-actions">
                <button
                  v-if="canEdit"
                  type="button"
                  class="icon-button"
                  @click="startEdit(item)"
                  aria-label="Edit expense"
                >
                  <v-icon icon="mdi-pencil" size="18" />
                </button>
                <button
                  v-if="canDelete"
                  type="button"
                  class="icon-button danger"
                  @click="removeRecord(item.id)"
                  aria-label="Delete expense"
                >
                  <v-icon icon="mdi-delete-outline" size="18" />
                </button>
              </div>
            </template>

            <template #no-data>
              <div class="empty-cell">No expense records found</div>
            </template>
          </v-data-table-server>
        </div>
      </section>
    </main>
  </DashboardLayout>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from "vue";
import PageMessage from "../components/common/PageMessage.vue";
import DashboardLayout from "../layouts/DashboardLayout.vue";
import { usePageMessage } from "../composables/usePageMessage";
import {
  createExpense,
  deleteExpense,
  getExpenses,
  updateExpense,
} from "../services/expensesApi";
import { expenseTypesApi, statusesApi } from "../services/tripSetupApi";
import {
  canCreateModule,
  canDeleteModule,
  canEditModule,
} from "../utils/authSession";

const moduleKey = "expenses";
const records = ref([]);
const totalRecords = ref(0);
const loading = ref(false);
const pageError = ref("");
const showForm = ref(false);
const editingId = ref(null);
const expenseTypeOptions = ref([
  "Fuel",
  "Toll",
  "Repair",
  "Parking",
  "Insurance",
  "Tax",
]);
const statusOptions = ref(["Active", "Pending", "Approved", "Paid"]);
const filters = reactive({ search: "", status: "", dateFrom: "", dateTo: "" });
const pagination = reactive({ page: 1, pageSize: 10 });
const form = reactive({
  expenseDate: "",
  expenseType: "",
  vehicleId: "",
  tripNumber: "",
  driverName: "",
  amount: 0,
  status: "Active",
  notes: "",
});
const { pageMessage, clearPageMessage, showPageMessage } = usePageMessage(4000);

const canCreate = computed(() => canCreateModule(moduleKey));
const canEdit = computed(() => canEditModule(moduleKey));
const canDelete = computed(() => canDeleteModule(moduleKey));
const expenseHeaders = [
  { title: "No.", key: "rowNumber", sortable: false },
  { title: "Date", key: "expenseDate", sortable: false },
  { title: "Type", key: "expenseType", sortable: false },
  { title: "Vehicle/ID", key: "vehicleId", sortable: false },
  { title: "Trip", key: "tripNumber", sortable: false },
  { title: "Driver", key: "driverName", sortable: false },
  { title: "Amount", key: "amount", align: "end", sortable: false },
  { title: "Status", key: "status", sortable: false },
  { title: "Actions", key: "actions", align: "end", sortable: false },
];

const loadOptions = async () => {
  try {
    const [types, statuses] = await Promise.all([
      expenseTypesApi.options(),
      statusesApi.options(),
    ]);
    if (types?.length) expenseTypeOptions.value = types;
    if (statuses?.length) statusOptions.value = statuses;
  } catch (error) {
    console.error(error);
  }
};

const loadRecords = async () => {
  pageError.value = "";
  loading.value = true;
  try {
    const result = await getExpenses({
      ...filters,
      page: pagination.page,
      pageSize: pagination.pageSize,
    });
    records.value = result?.items || [];
    totalRecords.value = result?.total || 0;

    const maxPage = Math.max(
      1,
      Math.ceil(totalRecords.value / pagination.pageSize),
    );
    if (pagination.page > maxPage) {
      pagination.page = maxPage;
      const retry = await getExpenses({
        ...filters,
        page: pagination.page,
        pageSize: pagination.pageSize,
      });
      records.value = retry?.items || [];
      totalRecords.value = retry?.total || 0;
    }
  } catch (error) {
    records.value = [];
    totalRecords.value = 0;
    pageError.value = error.message || "Could not load expenses.";
  } finally {
    loading.value = false;
  }
};

const totalPages = computed(() =>
  Math.max(1, Math.ceil(totalRecords.value / pagination.pageSize)),
);
const pageStart = computed(() =>
  totalRecords.value ? (pagination.page - 1) * pagination.pageSize + 1 : 0,
);
const tableRows = computed(() =>
  records.value.map((record, index) => ({
    ...record,
    rowNumber: pageStart.value + index,
  })),
);

const refreshRecords = async () => {
  pagination.page = 1;
  await loadRecords();
};

const handleTableOptions = async ({ page, itemsPerPage }) => {
  pagination.page = Math.min(Math.max(1, page || 1), totalPages.value);
  pagination.pageSize = itemsPerPage || 10;
  await loadRecords();
};

const resetForm = () => {
  Object.assign(form, {
    expenseDate: "",
    expenseType: expenseTypeOptions.value[0] || "",
    vehicleId: "",
    tripNumber: "",
    driverName: "",
    amount: 0,
    status: statusOptions.value[0] || "Active",
    notes: "",
  });
  editingId.value = null;
};

const startCreate = () => {
  resetForm();
  showForm.value = true;
};

const startEdit = (expense) => {
  Object.assign(form, {
    ...expense,
    notes: expense.notes || "",
    vehicleId: expense.vehicleId || "",
    tripNumber: expense.tripNumber || "",
    driverName: expense.driverName || "",
  });
  editingId.value = expense.id;
  showForm.value = true;
};

const cancelForm = () => {
  showForm.value = false;
  resetForm();
};

const saveRecord = async () => {
  const isEdit = Boolean(editingId.value);
  pageError.value = "";
  try {
    if (isEdit) await updateExpense(editingId.value, form);
    else await createExpense(form);
    showForm.value = false;
    resetForm();
    await loadRecords();
    showPageMessage({
      tone: "success",
      title: isEdit ? "Expense updated" : "Expense created",
      message: isEdit
        ? "Expense record was updated successfully."
        : "Expense record was created successfully.",
    });
  } catch (error) {
    pageError.value = error.message || "Could not save expense.";
    showPageMessage({
      tone: "error",
      title: "Expense was not saved",
      message: pageError.value,
    });
  }
};

const removeRecord = async (id) => {
  pageError.value = "";
  try {
    await deleteExpense(id);
    await loadRecords();
    showPageMessage({
      tone: "success",
      title: "Expense deleted",
      message: "Expense record was deleted successfully.",
    });
  } catch (error) {
    pageError.value = error.message || "Could not delete expense.";
    showPageMessage({
      tone: "error",
      title: "Expense was not deleted",
      message: pageError.value,
    });
  }
};

const formatCurrency = (value) =>
  new Intl.NumberFormat("en-US", { style: "currency", currency: "USD" }).format(
    Number(value || 0),
  );
const statusClass = (status) => {
  const normalized = String(status || "").toLowerCase();
  if (
    normalized === "active" ||
    normalized === "approved" ||
    normalized === "paid"
  )
    return "role-driver";
  if (normalized === "pending") return "role-mechanic";
  if (normalized === "rejected" || normalized === "cancelled")
    return "role-admin";
  return "role-dispatcher";
};

onMounted(async () => {
  await loadOptions();
  resetForm();
  await loadRecords();
});
</script>

<style scoped src="./page_styles/Expenses.css"></style>
