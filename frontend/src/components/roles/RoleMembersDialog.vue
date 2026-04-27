<template>
  <v-dialog :model-value="open" max-width="820" @update:model-value="updateOpen">
    <v-card class="dialog-card">
      <div class="dialog-header">
        <div>
          <h2>{{ role?.name }} Members</h2>
          <p class="text-muted">{{ loading ? 'Loading members...' : `${members.length} members` }}</p>
        </div>
        <button class="icon-button" type="button" @click="updateOpen(false)">
          <v-icon icon="mdi-close" />
        </button>
      </div>

      <div class="dialog-body">
        <div class="toolbar-search">
          <v-icon icon="mdi-magnify" />
          <input
            :value="search"
            type="text"
            placeholder="Search members..."
            @input="updateSearch"
          />
          <button
            v-if="search"
            class="clear-button"
            type="button"
            aria-label="Clear member search"
            @click="emit('update:search', '')"
          >
            <v-icon icon="mdi-close-circle" size="18" />
          </button>
        </div>

        <div class="card-surface table-card">
          <div class="table-wrap">
            <v-data-table
              class="table-base"
              :headers="headers"
              :items="members"
              :items-per-page="10"
              :sort-by="[]"
              :items-per-page-options="[10, 20, 30]"
              :mobile-breakpoint="0"
              :mobile="false"
              fixed-header
              height="360"
              density="comfortable"
            >
              <template #item.name="{ item }">
                <div class="name-cell">
                  <button
                    class="avatar avatar-button tooltip"
                    type="button"
                    @click="item.avatar && emit('view-avatar', item)"
                  >
                    <img v-if="item.avatar" :src="item.avatar" :alt="item.name" />
                    <span v-else>{{ initials(item.name) }}</span>
                    <span v-if="item.avatar" class="tooltip-text">View profile image</span>
                  </button>
                  <strong>{{ item.name }}</strong>
                </div>
              </template>

              <template #item.email="{ item }">
                <span class="text-muted">{{ item.email }}</span>
              </template>

              <template #item.phone="{ item }">
                <span class="text-muted">{{ item.phone }}</span>
              </template>

              <template #item.status="{ item }">
                <span class="badge" :class="item.status === 'Active' ? 'success' : 'neutral'">
                  {{ item.status }}
                </span>
              </template>

              <template #item.joinDate="{ item }">
                <span class="text-muted">{{ formatDate(item.joinDate) }}</span>
              </template>

              <template #no-data>
                <div class="empty-state">No members found for this role</div>
              </template>
            </v-data-table>
          </div>
        </div>
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
  role: {
    type: Object,
    default: null
  },
  members: {
    type: Array,
    required: true
  },
  headers: {
    type: Array,
    required: true
  },
  loading: {
    type: Boolean,
    default: false
  },
  search: {
    type: String,
    default: ''
  }
})

const emit = defineEmits(['update:open', 'update:search', 'view-avatar'])

const updateOpen = (value) => emit('update:open', value)

const updateSearch = (event) => emit('update:search', event.target.value)

const initials = (name) =>
  name
    .split(' ')
    .map((part) => part[0])
    .join('')

const formatDate = (value) =>
  new Date(value).toLocaleDateString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric'
  })
</script>

<style scoped src="./roles_styles/RoleMembersDialog.css"></style>
