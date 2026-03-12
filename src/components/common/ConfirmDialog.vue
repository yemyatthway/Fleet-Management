<template>
  <v-dialog v-model="internalOpen" max-width="420">
    <v-card class="confirm-card">
      <div class="confirm-header">
        <div class="icon" :class="toneClass">
          <v-icon :icon="icon" size="22" />
        </div>
        <div>
          <h3>{{ title }}</h3>
          <p>{{ message }}</p>
        </div>
      </div>
      <div class="confirm-actions">
        <button class="ghost" type="button" @click="cancel">{{ cancelText }}</button>
        <button class="primary" :class="toneClass" type="button" @click="confirm">
          {{ confirmText }}
        </button>
      </div>
    </v-card>
  </v-dialog>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  open: { type: Boolean, default: false },
  title: { type: String, default: 'Are you sure?' },
  message: { type: String, default: 'This action cannot be undone.' },
  confirmText: { type: String, default: 'Confirm' },
  cancelText: { type: String, default: 'Cancel' },
  tone: { type: String, default: 'danger' },
  icon: { type: String, default: 'mdi-alert-circle-outline' }
})

const emit = defineEmits(['confirm', 'cancel'])

const internalOpen = computed({
  get: () => props.open,
  set: (value) => {
    if (!value) emit('cancel')
  }
})

const toneClass = computed(() => (props.tone === 'warning' ? 'warning' : 'danger'))

const confirm = () => emit('confirm')
const cancel = () => emit('cancel')
</script>

<style scoped>
.confirm-card {
  border-radius: 18px;
  padding: 24px;
}

.confirm-header {
  display: flex;
  gap: 14px;
  align-items: flex-start;
}

.confirm-header h3 {
  margin: 0 0 6px;
  font-size: 18px;
}

.confirm-header p {
  margin: 0;
  color: var(--fleet-muted);
  font-size: 13px;
  line-height: 1.4;
}

.icon {
  width: 42px;
  height: 42px;
  border-radius: 12px;
  display: grid;
  place-items: center;
  flex-shrink: 0;
}

.icon.danger {
  background: #fee2e2;
  color: #dc2626;
}

.icon.warning {
  background: #ffedd5;
  color: #ea580c;
}

.confirm-actions {
  display: flex;
  gap: 10px;
  margin-top: 20px;
}

.confirm-actions button {
  flex: 1;
  border-radius: 12px;
  padding: 10px 12px;
  font-weight: 600;
  cursor: pointer;
  border: none;
}

.confirm-actions .ghost {
  background: #f8fafc;
  border: 1px solid var(--fleet-border);
  color: #334155;
}

.confirm-actions .primary {
  background: #dc2626;
  color: #fff;
}

.confirm-actions .primary.warning {
  background: #ea580c;
}
</style>
