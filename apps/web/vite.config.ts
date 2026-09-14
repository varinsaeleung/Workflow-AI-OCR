import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

/**
 * Configures Vite for the React web application and unit test runtime.
 */
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      "/api": "http://localhost:8080"
    }
  }
});
