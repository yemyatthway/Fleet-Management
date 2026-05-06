<template>
  <div class="login-page">
    <div class="login-bg">
      <img
        src="https://images.unsplash.com/photo-1755728531140-88e0b2a72d75?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=M3w3Nzg4Nzd8MHwxfHNlYXJjaHwxfHxsb2dpc3RpY3MlMjB0cnVja3MlMjBmbGVldCUyMHZlaGljbGVzfGVufDF8fHx8MTc3Mjg3OTUwMHww&ixlib=rb-4.1.0&q=80&w=1080&utm_source=figma&utm_medium=referral"
        alt="Fleet of logistics trucks"
      />
      <div class="login-overlay"></div>
    </div>

    <div class="login-card">
      <div class="logo-block">
        <div class="logo-icon">
          <v-icon icon="mdi-truck" size="28" />
        </div>
        <div>
          <h1>FleetManager</h1>
          <p>Logistics Platform</p>
        </div>
      </div>

      <div class="welcome">
        <h2>Welcome Back</h2>
        <p>Sign in to your account to continue</p>
      </div>

      <form class="login-form" @submit.prevent="handleSubmit">
        <div class="field">
          <label for="email">Email Address</label>
          <div class="field-input">
            <v-icon icon="mdi-email-outline" />
            <input id="email" v-model="email" type="email" placeholder="you@company.com" required />
          </div>
        </div>

        <div class="field">
          <label for="password">Password</label>
          <div class="field-input">
            <v-icon icon="mdi-lock-outline" />
            <input id="password" v-model="password" type="password" placeholder="••••••••" required />
          </div>
        </div>

        <div class="options">
          <label class="remember">
            <input type="checkbox" v-model="rememberMe" />
            Remember me
          </label>
          
        </div>

        <button class="submit" type="submit" :disabled="loading">
          {{ loading ? 'Signing In...' : 'Sign In' }}
        </button>
        <p v-if="errorMessage" class="login-error">{{ errorMessage }}</p>
      </form>
    </div>

    <div class="version">Fleet Management System v2.0</div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { login } from '../services/authApi'
import { setAuthSession } from '../utils/authSession'

const router = useRouter()
const email = ref('')
const password = ref('')
const rememberMe = ref(false)
const loading = ref(false)
const errorMessage = ref('')

const handleSubmit = async () => {
  loading.value = true
  errorMessage.value = ''
  try {
    const session = await login({ email: email.value, password: password.value })
    setAuthSession(session)
    router.push('/dashboard')
  } catch (error) {
    errorMessage.value = error.message
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: grid;
  place-items: center;
  position: relative;
  overflow: hidden;
  padding: 24px;
}

.login-bg {
  position: absolute;
  inset: 0;
  z-index: 0;
}

.login-bg img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.login-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, rgba(15, 23, 42, 0.65), rgba(37, 99, 235, 0.35));
  backdrop-filter: blur(6px);
}

.login-card {
  position: relative;
  z-index: 1;
  width: min(420px, 100%);
  background: #fff;
  border-radius: 24px;
  padding: 32px;
  box-shadow: 0 24px 60px rgba(15, 23, 42, 0.35);
}

.logo-block {
  display: flex;
  align-items: center;
  gap: 12px;
  justify-content: center;
  margin-bottom: 24px;
  text-align: left;
}

.logo-icon {
  width: 52px;
  height: 52px;
  border-radius: 16px;
  display: grid;
  place-items: center;
  color: #fff;
  background: linear-gradient(135deg, #2563eb, #1e40af);
  box-shadow: 0 8px 18px rgba(37, 99, 235, 0.35);
}

.logo-block h1 {
  font-size: 22px;
  margin: 0;
}

.logo-block p {
  margin: 2px 0 0;
  color: var(--fleet-muted);
  font-size: 13px;
}

.welcome {
  text-align: center;
  margin-bottom: 24px;
}

.welcome h2 {
  margin: 0 0 8px;
}

.welcome p {
  margin: 0;
  color: var(--fleet-muted);
}

.login-form {
  display: flex;
  flex-direction: column;
  gap: 18px;
}

.field label {
  display: block;
  font-size: 13px;
  font-weight: 600;
  color: #334155;
  margin-bottom: 8px;
}

.field-input {
  display: flex;
  align-items: center;
  gap: 8px;
  border: 1px solid var(--fleet-border);
  border-radius: 12px;
  padding: 10px 12px;
  background: #fff;
}

.field-input input {
  border: none;
  outline: none;
  flex: 1;
  font-size: 14px;
}

.options {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 13px;
}

.remember {
  display: flex;
  gap: 8px;
  align-items: center;
  color: #334155;
}

.remember input {
  accent-color: var(--fleet-primary);
}

.link {
  color: var(--fleet-primary);
  font-weight: 600;
}

.submit {
  border: none;
  border-radius: 12px;
  padding: 12px;
  color: #fff;
  font-weight: 600;
  cursor: pointer;
  background: linear-gradient(90deg, #2563eb, #1e40af);
  box-shadow: 0 10px 20px rgba(37, 99, 235, 0.3);
}

.submit:hover {
  background: linear-gradient(90deg, #1e40af, #1e3a8a);
}

.submit:disabled {
  cursor: not-allowed;
  opacity: 0.65;
}

.login-error {
  margin: -4px 0 0;
  color: var(--fleet-danger);
  font-size: 13px;
  font-weight: 600;
  text-align: center;
}

.footer {
  margin-top: 20px;
  text-align: center;
  font-size: 13px;
  color: var(--fleet-muted);
}

.footer strong {
  color: #0f172a;
}

.seed-users {
  line-height: 1.5;
}

.version {
  position: relative;
  z-index: 1;
  margin-top: 18px;
  color: rgba(255, 255, 255, 0.85);
  font-size: 13px;
  text-shadow: 0 6px 12px rgba(15, 23, 42, 0.45);
}
</style>
