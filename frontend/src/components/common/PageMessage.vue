<template>
  <div v-if="message" class="page-message" :class="toneClass" role="status" aria-live="polite">
    <div class="message-icon">
      <v-icon :icon="icon" size="20" />
    </div>
    <div class="message-content">
      <div class="message-title">{{ title }}</div>
      <div class="message-text">{{ message }}</div>
    </div>
    <button class="message-close" type="button" aria-label="Close message" @click="$emit('close')">
      <v-icon icon="mdi-close" size="18" />
    </button>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  tone: {
    type: String,
    default: 'info'
  },
  title: {
    type: String,
    default: ''
  },
  message: {
    type: String,
    default: ''
  }
})

defineEmits(['close'])

const toneMap = {
  success: {
    title: 'Success',
    icon: 'mdi-check-circle-outline'
  },
  warning: {
    title: 'Warning',
    icon: 'mdi-alert-outline'
  },
  error: {
    title: 'Something went wrong',
    icon: 'mdi-alert-circle-outline'
  },
  info: {
    title: 'Notice',
    icon: 'mdi-information-outline'
  }
}

const normalizedTone = computed(() => toneMap[props.tone] ? props.tone : 'info')
const toneClass = computed(() => `tone-${normalizedTone.value}`)
const icon = computed(() => toneMap[normalizedTone.value].icon)
const title = computed(() => props.title || toneMap[normalizedTone.value].title)
</script>

<style scoped>
.page-message {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  padding: 14px 16px;
  border: 1px solid var(--message-border);
  border-radius: 16px;
  background:
    linear-gradient(135deg, rgba(255, 255, 255, 0.92), rgba(255, 255, 255, 0.78)),
    var(--message-bg);
  color: var(--message-color);
  box-shadow: 0 18px 40px rgba(15, 23, 42, 0.08);
}

.message-icon {
  width: 42px;
  height: 42px;
  flex: 0 0 42px;
  border-radius: 12px;
  display: grid;
  place-items: center;
  background: var(--message-icon-bg);
  color: var(--message-icon-color);
}

.message-content {
  min-width: 0;
  flex: 1;
  padding-top: 1px;
}

.message-title {
  font-size: 14px;
  font-weight: 800;
  color: #0f172a;
}

.message-text {
  margin-top: 3px;
  font-size: 13px;
  line-height: 1.45;
  color: var(--message-text);
}

.message-close {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 auto;
  width: 34px;
  height: 34px;
  border: none;
  border-radius: 10px;
  background: transparent;
  color: var(--message-icon-color);
  cursor: pointer;
}

.message-close:hover {
  background: var(--message-close-bg);
}

.tone-success {
  --message-bg: #dcfce7;
  --message-border: #bbf7d0;
  --message-color: #166534;
  --message-text: #15803d;
  --message-icon-bg: #dcfce7;
  --message-icon-color: #16a34a;
  --message-close-bg: #dcfce7;
}

.tone-warning {
  --message-bg: #ffedd5;
  --message-border: #fed7aa;
  --message-color: #9a3412;
  --message-text: #c2410c;
  --message-icon-bg: #ffedd5;
  --message-icon-color: #ea580c;
  --message-close-bg: #ffedd5;
}

.tone-error {
  --message-bg: #fee2e2;
  --message-border: #fecaca;
  --message-color: #991b1b;
  --message-text: #b91c1c;
  --message-icon-bg: #fee2e2;
  --message-icon-color: #dc2626;
  --message-close-bg: #fee2e2;
}

.tone-info {
  --message-bg: #dbeafe;
  --message-border: #bfdbfe;
  --message-color: #1e40af;
  --message-text: #2563eb;
  --message-icon-bg: #dbeafe;
  --message-icon-color: #2563eb;
  --message-close-bg: #dbeafe;
}

@media (max-width: 560px) {
  .page-message {
    padding: 12px;
  }

  .message-icon {
    width: 38px;
    height: 38px;
    flex-basis: 38px;
  }
}
</style>
