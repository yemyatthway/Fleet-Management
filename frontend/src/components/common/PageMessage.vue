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

<style scoped src="./common_styles/PageMessage.css"></style>
