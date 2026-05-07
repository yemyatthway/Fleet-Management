import { createApp } from "vue";
import App from "./App.vue";
import router from "./router";
import "vuetify/styles";
import "@mdi/font/css/materialdesignicons.css";
import { createVuetify } from "vuetify";
import * as components from "vuetify/components";
import * as directives from "vuetify/directives";
import "./style.css";

const vuetify = createVuetify({
  components,
  directives,
  theme: {
    defaultTheme: "fleetLight",
    themes: {
      fleetLight: {
        dark: false,
        colors: {
          background: "#f5f7fb",
          surface: "#ffffff",
          primary: "#2563eb",
          secondary: "#1e40af",
          info: "#3b82f6",
          success: "#10b981",
          warning: "#f59e0b",
          error: "#ef4444",
        },
      },
    },
  },
});

createApp(App).use(router).use(vuetify).mount("#app");
