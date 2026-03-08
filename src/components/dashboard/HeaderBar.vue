<template>
  <v-app-bar flat height="64" class="header-bar" position="static">
    <div class="header-left">
      <v-btn icon variant="text" class="icon-button" @click="$emit('toggle')">
        <v-icon icon="mdi-menu" />
      </v-btn>
      <div class="search-box">
        <v-icon icon="mdi-magnify" class="search-icon" />
        <input
          class="search-input"
          type="text"
          placeholder="Search vehicles, drivers, trips..."
        />
      </div>
    </div>

    <div class="header-right">
      <button class="notify-button" type="button">
        <v-icon icon="mdi-bell-outline" />
        <span class="notify-dot"></span>
      </button>
      <div class="date-label">{{ today }}</div>
    </div>
  </v-app-bar>
</template>

<script setup>
import { computed } from 'vue'

defineEmits(['toggle'])

const today = computed(() =>
  new Date().toLocaleDateString('en-US', {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric'
  })
)
</script>

<style scoped>
.header-bar {
  background: #fff;
  border-bottom: 1px solid var(--fleet-border);
  padding: 0 24px;
}

.header-left,
.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.header-left {
  flex: 1;
}

.icon-button {
  border-radius: 12px;
}

.search-box {
  position: relative;
  max-width: 420px;
  width: 100%;
}

.search-input {
  width: 100%;
  padding: 10px 16px 10px 44px;
  border: 1px solid var(--fleet-border);
  border-radius: 12px;
  font-size: 14px;
  background: #fff;
}

.search-input:focus {
  outline: 2px solid rgba(37, 99, 235, 0.2);
  border-color: rgba(37, 99, 235, 0.5);
}

.search-icon {
  position: absolute;
  left: 14px;
  top: 50%;
  transform: translateY(-50%);
  color: #94a3b8;
  font-size: 20px;
}

.notify-button {
  position: relative;
  border: none;
  background: #fff;
  width: 40px;
  height: 40px;
  border-radius: 12px;
  display: grid;
  place-items: center;
  cursor: pointer;
}

.notify-button:hover {
  background: #f8fafc;
}

.notify-dot {
  position: absolute;
  top: 9px;
  right: 10px;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--fleet-danger);
}

.date-label {
  color: var(--fleet-muted);
  font-size: 14px;
}

@media (max-width: 900px) {
  .search-box {
    display: none;
  }
}
</style>
