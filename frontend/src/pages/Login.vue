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
        <h2>{{ otpRequired ? "Verify Login" : "Welcome Back" }}</h2>
        <p>
          {{ otpRequired ? otpMessage : "Sign in to your account to continue" }}
        </p>
      </div>

      <form
        v-if="!otpRequired"
        class="login-form"
        @submit.prevent="handleSubmit"
      >
        <div class="field">
          <label for="email">Email Address</label>
          <div class="field-input">
            <v-icon icon="mdi-email-outline" />
            <input
              id="email"
              v-model="email"
              type="email"
              placeholder="you@company.com"
              required
            />
          </div>
        </div>

        <div class="field">
          <label for="password">Password</label>
          <div class="field-input">
            <v-icon icon="mdi-lock-outline" />
            <input
              id="password"
              v-model="password"
              type="password"
              placeholder="••••••••"
              required
            />
          </div>
        </div>

        <div class="options">
          <label class="remember">
            <input type="checkbox" v-model="rememberMe" />
            Remember me
          </label>
        </div>

        <button class="submit" type="submit" :disabled="loading">
          {{ loading ? "Signing In..." : "Sign In" }}
        </button>
        <p v-if="errorMessage" class="login-error">{{ errorMessage }}</p>
      </form>

      <form v-else class="login-form" @submit.prevent="handleOtpSubmit">
        <div class="field">
          <label for="otp">Email OTP</label>
          <div class="field-input">
            <v-icon icon="mdi-shield-key-outline" />
            <input
              id="otp"
              v-model="otpCode"
              inputmode="numeric"
              maxlength="6"
              placeholder="123456"
              required
            />
          </div>
        </div>

        <button class="submit" type="submit" :disabled="loading">
          {{ loading ? "Verifying..." : "Verify & Sign In" }}
        </button>
        <button
          class="secondary-submit"
          type="button"
          :disabled="loading"
          @click="resetOtp"
        >
          Back to sign in
        </button>
        <p v-if="errorMessage" class="login-error">{{ errorMessage }}</p>
      </form>
    </div>

    <div class="version">Fleet Management System v2.0</div>
  </div>
</template>

<script setup>
import { onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { login, verifyOtp } from "../services/authApi";
import {
  clearRememberedLogin,
  getRememberedLogin,
  setAuthSession,
  setRememberedLogin,
} from "../utils/authSession";

const router = useRouter();
const email = ref("");
const password = ref("");
const rememberMe = ref(false);
const loading = ref(false);
const errorMessage = ref("");
const otpRequired = ref(false);
const otpCode = ref("");
const otpChallengeId = ref("");
const otpMessage = ref("");

const persistRememberedLogin = () => {
  if (rememberMe.value) {
    setRememberedLogin(email.value);
  } else {
    clearRememberedLogin();
  }
};

const handleSubmit = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const session = await login({
      email: email.value,
      password: password.value,
      rememberMe: rememberMe.value,
    });
    persistRememberedLogin();
    if (session.requiresTwoFactor) {
      otpRequired.value = true;
      otpChallengeId.value = session.challengeId;
      otpMessage.value =
        session.message || "Enter the verification code sent to your email.";
      return;
    }
    setAuthSession(session, rememberMe.value);
    router.push("/dashboard");
  } catch (error) {
    errorMessage.value = error.message;
  } finally {
    loading.value = false;
  }
};

const handleOtpSubmit = async () => {
  loading.value = true;
  errorMessage.value = "";
  try {
    const session = await verifyOtp({
      challengeId: otpChallengeId.value,
      code: otpCode.value,
    });
    persistRememberedLogin();
    setAuthSession(session, rememberMe.value);
    router.push("/dashboard");
  } catch (error) {
    errorMessage.value = error.message;
  } finally {
    loading.value = false;
  }
};

const resetOtp = () => {
  otpRequired.value = false;
  otpCode.value = "";
  otpChallengeId.value = "";
  otpMessage.value = "";
  errorMessage.value = "";
};

onMounted(() => {
  const rememberedLogin = getRememberedLogin();
  if (rememberedLogin?.email) {
    email.value = rememberedLogin.email;
    rememberMe.value = true;
  }
});
</script>

<style scoped src="./page_styles/Login.css"></style>
