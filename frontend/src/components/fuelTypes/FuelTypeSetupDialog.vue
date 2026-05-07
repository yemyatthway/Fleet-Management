<template>
  <v-dialog v-model="internalOpen" max-width="560">
    <v-card class="dialog-card">
      <div class="dialog-header">
        <h2>{{ mode === "edit" ? `Edit ${label}` : `Create ${label}` }}</h2>
        <button class="icon-button" type="button" @click="close">
          <v-icon icon="mdi-close" />
        </button>
      </div>

      <form class="dialog-body" @submit.prevent="submit">
        <div class="field">
          <label class="required">Name</label>
          <input
            v-model.trim="form.name"
            type="text"
            placeholder="Diesel"
            required
          />
        </div>

        <div class="field">
          <label class="required">Code</label>
          <input
            v-model.trim="form.code"
            type="text"
            placeholder="FT-DIESEL"
            required
          />
        </div>

        <div class="field">
          <label>Description</label>
          <textarea
            v-model.trim="form.description"
            rows="3"
            placeholder="Standard diesel fuel for commercial vehicles."
          />
        </div>

        <div class="field">
          <label class="required">Status</label>
          <select v-model="form.status">
            <option value="Active">Active</option>
            <option value="Disabled">Disabled</option>
          </select>
        </div>

        <p v-if="formError" class="form-error">{{ formError }}</p>

        <div class="dialog-actions">
          <button class="ghost" type="button" @click="close">Cancel</button>
          <button class="primary" type="submit">
            {{ mode === "edit" ? "Save Changes" : `Create ${label}` }}
          </button>
        </div>
      </form>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { computed, reactive, ref, watch } from "vue";

const props = defineProps({
  open: {
    type: Boolean,
    default: false,
  },
  mode: {
    type: String,
    default: "add",
  },
  item: {
    type: Object,
    default: null,
  },
  label: {
    type: String,
    default: "Fuel Type",
  },
});

const emit = defineEmits(["close", "save"]);

const internalOpen = computed({
  get: () => props.open,
  set: (value) => {
    if (!value) emit("close");
  },
});

const form = reactive({
  id: "",
  name: "",
  code: "",
  description: "",
  status: "Active",
});

const formError = ref("");

const reset = () => {
  form.id = props.item?.id || "";
  form.name = props.item?.name || "";
  form.code = props.item?.code || "";
  form.description = props.item?.description || "";
  form.status = props.item?.status || "Active";
  formError.value = "";
};

watch(
  () => props.open,
  (value) => {
    if (value) reset();
  },
);

const close = () => emit("close");

const submit = () => {
  if (!form.name || !form.code || !form.status) {
    formError.value = "Please complete all required fields.";
    return;
  }

  formError.value = "";
  emit("save", { ...form });
};
</script>

<style scoped src="../roles/roles_styles/RoleDialog.css"></style>
