import { beforeEach, describe, expect, it, vi } from "vitest";

const fetchSpy = vi.fn();

vi.stubGlobal("fetch", fetchSpy);

/**
 * Imports the API module after setting demo mode so import-time config is deterministic.
 */
async function importApiWithDemoMode(enabled: boolean) {
  vi.resetModules();
  vi.stubEnv("VITE_DEMO_MODE", enabled ? "true" : "false");
  return import("./api");
}

describe("api demo mode", () => {
  beforeEach(() => {
    fetchSpy.mockReset();
    window.sessionStorage.clear();
  });

  /**
   * Catches the bug where demo login still calls the backend and cannot enter the app without Docker.
   */
  it("login should return a demo session without calling the backend when demo mode is enabled", async () => {
    const api = await importApiWithDemoMode(true);

    const session = await api.login("admin@km.local", "ChangeMe!2026");

    expect(session.user.email).toBe("admin@km.local");
    expect(session.user.displayName).toBe("Demo Administrator");
    expect(session.permissions).toContain("dashboard.read");
    expect(api.readStoredSession()?.accessToken).toBe("demo-access-token");
    expect(fetchSpy).not.toHaveBeenCalled();
  });

  /**
   * Catches the bug where dashboard and search screens are empty in demo mode.
   */
  it("dashboard and document search should return realistic demo data", async () => {
    const api = await importApiWithDemoMode(true);

    const dashboard = await api.getEnterpriseDashboard();
    const documents = await api.searchDocuments("invoice");

    expect(dashboard.ocr.completed).toBeGreaterThan(0);
    expect(dashboard.workflow.pendingTasks).toBeGreaterThan(0);
    expect(documents).toHaveLength(1);
    expect(documents[0].fileName).toContain("Invoice");
    expect(fetchSpy).not.toHaveBeenCalled();
  });
});
