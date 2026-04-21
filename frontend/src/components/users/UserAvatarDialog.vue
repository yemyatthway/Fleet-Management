<template>
  <v-dialog :model-value="open" max-width="720" @update:model-value="updateOpen">
    <v-card class="dialog-card">
      <div class="dialog-header">
        <h2>{{ name }}</h2>
        <button class="icon-button" type="button" @click="updateOpen(false)">
          <v-icon icon="mdi-close" />
        </button>
      </div>
      <div class="dialog-body">
        <img v-if="url" class="avatar-preview" :src="url" :alt="name" />
      </div>
    </v-card>
  </v-dialog>
</template>

<script setup>
defineProps({
  open: {
    type: Boolean,
    default: false
  },
  name: {
    type: String,
    default: ''
  },
  url: {
    type: String,
    default: ''
  }
})

const emit = defineEmits(['update:open'])

const updateOpen = (value) => emit('update:open', value)
</script>

<style scoped src="./users_styles/UserAvatarDialog.css"></style>
