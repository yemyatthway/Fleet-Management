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

<style scoped src="./common_styles/ConfirmDialog.css"></style>
