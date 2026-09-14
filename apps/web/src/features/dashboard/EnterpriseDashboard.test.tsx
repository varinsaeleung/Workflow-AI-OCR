import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { EnterpriseDashboard } from "./EnterpriseDashboard";
import type { EnterpriseDashboardDto } from "../../types/api";

const dashboard: EnterpriseDashboardDto = {
  ocr: { total: 10, queued: 1, completed: 8, failed: 1, completionRate: 80 },
  workflow: { inProgress: 4, approved: 7, rejected: 2, pendingTasks: 3 },
  users: { activeUsers: 12, lockedUsers: 1, roleCount: 4 },
  errors: { totalErrors: 2, ocrErrors: 1, aiErrors: 1 },
  performance: { averageOcrConfidence: 0.91, documentsPerDay: 14, averageProcessingSeconds: 280 },
  storage: { documentCount: 20, totalBytes: 2048, versionCount: 25 },
  throughput: [{ label: "2026-09-11", value: 5 }]
};

describe("EnterpriseDashboard", () => {
  /**
   * Ensures the enterprise dashboard shows all required operational sections.
   */
  it("renders OCR, workflow, users, errors, performance, and storage metrics", () => {
    render(<EnterpriseDashboard dashboard={dashboard} />);

    expect(screen.getByRole("heading", { name: "Enterprise Dashboard" })).toBeInTheDocument();
    expect(screen.getAllByText("OCR")).not.toHaveLength(0);
    expect(screen.getAllByText("Workflow")).not.toHaveLength(0);
    expect(screen.getByText("Users")).toBeInTheDocument();
    expect(screen.getByText("Errors")).toBeInTheDocument();
    expect(screen.getByText("Performance")).toBeInTheDocument();
    expect(screen.getByText("Storage")).toBeInTheDocument();
    expect(screen.getByText("80%")).toBeInTheDocument();
    expect(screen.getByText("2 KB")).toBeInTheDocument();
  });
});
