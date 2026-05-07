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

<style scoped>
.profile-page { padding: 28px 32px; display: grid; gap: 20px; }
.profile-header h1 { margin: 0; font-size: 28px; }
.profile-header p { margin: 6px 0 0; color: #64748b; }
.profile-grid { display: grid; grid-template-columns: minmax(0, 1.3fr) minmax(320px, 0.7fr); gap: 20px; align-items: start; }
.profile-card-main, .password-card { padding: 22px; border: 1px solid var(--fleet-border); border-radius: 16px; background: #fff; }
.identity-row { display: flex; align-items: center; gap: 16px; padding-bottom: 18px; border-bottom: 1px solid var(--fleet-border); }
.profile-avatar { width: 74px; height: 74px; border-radius: 18px; object-fit: cover; }
.avatar-fallback { display: grid; place-items: center; background: #2563eb; color: #fff; font-weight: 800; font-size: 22px; }
h2 { margin: 0; font-size: 20px; }
.identity-row p, .password-card p { margin: 4px 0 0; color: #64748b; }
.info-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 14px; padding-top: 18px; }
.info-item { display: grid; gap: 4px; padding: 14px; border: 1px solid #e2e8f0; border-radius: 12px; background: #f8fafc; }
.info-item span, label { color: #64748b; font-size: 13px; }
.info-item strong { color: #0f172a; overflow-wrap: anywhere; }
.password-card { display: grid; gap: 14px; }
label { display: grid; gap: 7px; font-weight: 700; }
input { width: 100%; min-height: 44px; border: 1px solid #dfe3ea; border-radius: 10px; padding: 0 12px; font: inherit; background: #fff; box-sizing: border-box; }
.password-field { position: relative; display: block; }
.password-field input { padding-right: 46px; }
.password-toggle { position: absolute; top: 50%; right: 8px; transform: translateY(-50%); width: 34px; height: 34px; border: 0; border-radius: 9px; display: inline-flex; align-items: center; justify-content: center; color: #64748b; background: transparent; cursor: pointer; }
.password-toggle:hover { background: #eff6ff; color: #2563eb; }
.password-toggle:focus-visible { outline: 2px solid rgba(37, 99, 235, 0.35); outline-offset: 2px; }
.primary-button { min-height: 44px; border: 0; border-radius: 10px; padding: 0 18px; font-weight: 700; display: inline-flex; align-items: center; justify-content: center; gap: 8px; cursor: pointer; background: #2563eb; color: white; }
.primary-button:disabled { opacity: 0.6; cursor: not-allowed; }
.page-message { padding: 12px 14px; border-radius: 12px; border: 1px solid #dbe3ef; background: #fff; }
.page-message.success { border-color: #bbf7d0; background: #f0fdf4; color: #15803d; }
.page-message.error { border-color: #fecaca; background: #fef2f2; color: #b91c1c; }
@media (max-width: 900px) { .profile-grid { grid-template-columns: 1fr; } .profile-page { padding: 20px; } }
@media (max-width: 560px) { .info-grid { grid-template-columns: 1fr; } .identity-row { align-items: flex-start; } }
</style>
