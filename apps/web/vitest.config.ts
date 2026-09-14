import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

/**
 * Configures Vitest for React component unit tests.
 */
export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    maxWorkers: 1,
    pool: "vmThreads",
    setupFiles: "./src/setupTests.ts"
  }
});
