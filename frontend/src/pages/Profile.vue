<template>
  <DashboardLayout>
    <main class="profile-page">
      <header class="profile-header">
        <div>
          <h1>Profile</h1>
          <p>Review your account and update your login password.</p>
        </div>
      </header>

      <div v-if="message.text" class="page-message" :class="message.tone">{{ message.text }}</div>

      <section class="profile-grid">
        <div class="card-surface profile-card-main">
          <div class="identity-row">
            <img v-if="profile?.avatar" :src="profile.avatar" :alt="profile.name" class="profile-avatar" />
            <div v-else class="profile-avatar avatar-fallback">{{ initials }}</div>
            <div>
              <h2>{{ profile?.name || currentUser?.name || 'Fleet User' }}</h2>
              <p>{{ profile?.role || currentUser?.role || '-' }}</p>
            </div>
          </div>

          <div class="info-grid">
            <div class="info-item">
              <span>Email</span>
              <strong>{{ profile?.email || currentUser?.email || '-' }}</strong>
            </div>
            <div class="info-item">
              <span>Employee ID</span>
              <strong>{{ profile?.employeeId || '-' }}</strong>
            </div>
            <div class="info-item">
              <span>Phone</span>
              <strong>{{ profile?.phone || '-' }}</strong>
            </div>
            <div class="info-item">
              <span>Status</span>
              <strong>{{ profile?.status || '-' }}</strong>
            </div>
            <div class="info-item">
              <span>Department</span>
              <strong>{{ profile?.department || '-' }}</strong>
            </div>
            <div class="info-item">
              <span>Title</span>
              <strong>{{ profile?.title || '-' }}</strong>
            </div>
          </div>
        </div>

        <form class="card-surface password-card" @submit.prevent="savePassword">
          <div>
            <h2>Change Password</h2>
            <p>Use this after logging in with the default password.</p>
          </div>

          <label>
            Current Password
            <span class="password-field">
              <input
                v-model="passwordForm.currentPassword"
                :type="passwordVisibility.currentPassword ? 'text' : 'password'"
                autocomplete="current-password"
                required
              />
              <button
                class="password-toggle"
                type="button"
                :aria-label="passwordVisibility.currentPassword ? 'Hide current password' : 'Show current password'"
                @click="passwordVisibility.currentPassword = !passwordVisibility.currentPassword"
              >
                <v-icon :icon="passwordVisibility.currentPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'" size="20" />
              </button>
            </span>
          </label>

          <label>
            New Password
            <span class="password-field">
              <input
                v-model="passwordForm.newPassword"
                :type="passwordVisibility.newPassword ? 'text' : 'password'"
                autocomplete="new-password"
                required
              />
              <button
                class="password-toggle"
                type="button"
                :aria-label="passwordVisibility.newPassword ? 'Hide new password' : 'Show new password'"
                @click="passwordVisibility.newPassword = !passwordVisibility.newPassword"
              >
                <v-icon :icon="passwordVisibility.newPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'" size="20" />
              </button>
            </span>
          </label>

          <label>
            Confirm Password
            <span class="password-field">
              <input
                v-model="passwordForm.confirmPassword"
                :type="passwordVisibility.confirmPassword ? 'text' : 'password'"
                autocomplete="new-password"
                required
              />
              <button
                class="password-toggle"
                type="button"
                :aria-label="passwordVisibility.confirmPassword ? 'Hide confirm password' : 'Show confirm password'"
                @click="passwordVisibility.confirmPassword = !passwordVisibility.confirmPassword"
              >
                <v-icon :icon="passwordVisibility.confirmPassword ? 'mdi-eye-off-outline' : 'mdi-eye-outline'" size="20" />
              </button>
            </span>
          </label>

          <button class="primary-button" type="submit" :disabled="saving">
            <v-icon icon="mdi-lock-reset" size="18" />
            {{ saving ? 'Saving...' : 'Update Password' }}
          </button>
        </form>
      </section>
    </main>
  </DashboardLayout>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import DashboardLayout from '../layouts/DashboardLayout.vue'
import { changePassword, getProfile } from '../services/profileApi'
import { getAuthSession, getCurrentUser, setAuthSession } from '../utils/authSession'

const currentUser = computed(() => getCurrentUser())
const profile = ref(null)
const saving = ref(false)
const message = reactive({ tone: '', text: '' })
const passwordForm = reactive({ currentPassword: '', newPassword: '', confirmPassword: '' })
const passwordVisibility = reactive({ currentPassword: false, newPassword: false, confirmPassword: false })
let messageTimer = null

const initials = computed(() =>
  String(profile.value?.name || currentUser.value?.name || 'FU')
    .split(' ')
    .map((part) => part[0])
    .join('')
    .slice(0, 2)
    .toUpperCase()
)

const showMessage = (tone, text) => {
  if (messageTimer) {
    window.clearTimeout(messageTimer)
  }

  message.tone = tone
  message.text = text
  messageTimer = window.setTimeout(() => {
    message.tone = ''
    message.text = ''
    messageTimer = null
  }, 4000)
}

const loadProfile = async () => {
  try {
    profile.value = await getProfile()
    const session = getAuthSession()
    if (session) {
      setAuthSession({
        ...session,
        user: {
          ...session.user,
          name: profile.value.name,
          email: profile.value.email,
          role: profile.value.role,
          status: profile.value.status,
          avatar: profile.value.avatar
        }
      }, Boolean(session.remember))
    }
  } catch (error) {
    showMessage('error', error.message || 'Could not load profile.')
  }
}

const resetPasswordForm = () => {
  passwordForm.currentPassword = ''
  passwordForm.newPassword = ''
  passwordForm.confirmPassword = ''
  passwordVisibility.currentPassword = false
  passwordVisibility.newPassword = false
  passwordVisibility.confirmPassword = false
}

const savePassword = async () => {
  saving.value = true
  try {
    await changePassword(passwordForm)
    resetPasswordForm()
    showMessage('success', 'Password changed successfully.')
  } catch (error) {
    showMessage('error', error.message || 'Could not change password.')
  } finally {
    saving.value = false
  }
}

onMounted(loadProfile)
</script>

<style scoped src="./page_styles/Profile.css"></style>
