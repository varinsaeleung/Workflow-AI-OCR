import type { DashboardSummary, DocumentDto, EnterpriseDashboardDto } from "../types/api";
import type { AuthSession } from "./api";

/**
 * Builds a deterministic authenticated session for local frontend demonstrations.
 */
export function createDemoSession(email: string): AuthSession {
  return {
    accessToken: "demo-access-token",
    refreshToken: "demo-refresh-token",
    accessTokenExpiresAt: new Date(Date.now() + 60 * 60 * 1000).toISOString(),
    user: {
      id: "00000000-0000-0000-0000-000000000001",
      email: email.trim() || "admin@km.local",
      displayName: "Demo Administrator"
    },
    permissions: [
      "documents.read",
      "documents.write",
      "documents.ai",
      "documents.delete",
      "workflow.read",
      "workflow.write",
      "workflow.approve",
      "dashboard.read",
      "admin.users"
    ]
  };
}

/**
 * Returns summary numbers that match the demo document and workflow dataset.
 */
export function getDemoDashboardSummary(): DashboardSummary {
  return {
    totalDocuments: 6,
    ocrQueued: 1,
    ocrCompleted: 4,
    inWorkflow: 2,
    approved: 2,
    rejected: 1
  };
}

/**
 * Returns chart and metric data for the enterprise operations dashboard.
 */
export function getDemoEnterpriseDashboard(): EnterpriseDashboardDto {
  return {
    ocr: { total: 6, queued: 1, completed: 4, failed: 1, completionRate: 66.67 },
    workflow: { inProgress: 2, approved: 2, rejected: 1, pendingTasks: 3 },
    users: { activeUsers: 87, lockedUsers: 3, roleCount: 5 },
    errors: { totalErrors: 3, ocrErrors: 1, aiErrors: 2 },
    performance: { averageOcrConfidence: 0.91, documentsPerDay: 18, averageProcessingSeconds: 42 },
    storage: { documentCount: 6, totalBytes: 73400320, versionCount: 9 },
    throughput: [
      { label: "2026-09-10", value: 8 },
      { label: "2026-09-11", value: 13 },
      { label: "2026-09-12", value: 10 },
      { label: "2026-09-13", value: 18 },
      { label: "2026-09-14", value: 15 },
      { label: "2026-09-15", value: 21 }
    ]
  };
}

/**
 * Returns demo documents with OCR, AI, metadata, and workflow-oriented status values.
 */
export function getDemoDocuments(): DocumentDto[] {
  return [
    {
      id: "11111111-1111-1111-1111-111111111111",
      fileName: "Invoice-KM-2026-0001.pdf",
      contentType: "application/pdf",
      documentType: "invoice",
      status: "OcrCompleted",
      uploadedBy: "admin@km.local",
      storagePath: "minio://documents/invoices/Invoice-KM-2026-0001.pdf",
      folderId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      storageBucket: "documents",
      fileSizeBytes: 2467840,
      currentVersionNumber: 2,
      deletedAt: null,
      createdAt: "2026-09-15T01:45:00Z",
      metadata: { vendor: "Konica Minolta", department: "Finance", amount: "125000" },
      ocrText: "Tax invoice for managed print services",
      ocrConfidence: 0.96,
      aiClassification: "Invoice",
      aiSummary: "Invoice awaiting finance verification."
    },
    {
      id: "22222222-2222-2222-2222-222222222222",
      fileName: "Contract-Service-Renewal.pdf",
      contentType: "application/pdf",
      documentType: "contract",
      status: "InWorkflow",
      uploadedBy: "legal@km.local",
      storagePath: "minio://documents/contracts/Contract-Service-Renewal.pdf",
      folderId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      storageBucket: "documents",
      fileSizeBytes: 5242880,
      currentVersionNumber: 1,
      deletedAt: null,
      createdAt: "2026-09-14T07:20:00Z",
      metadata: { customer: "Bangkok Office", owner: "Legal", priority: "High" },
      ocrText: "Service renewal agreement",
      ocrConfidence: 0.89,
      aiClassification: "Contract",
      aiSummary: "Renewal contract currently waiting for approval."
    },
    {
      id: "33333333-3333-3333-3333-333333333333",
      fileName: "Receipt-Office-Supplies.png",
      contentType: "image/png",
      documentType: "receipt",
      status: "OcrQueued",
      uploadedBy: "office@km.local",
      storagePath: "minio://documents/receipts/Receipt-Office-Supplies.png",
      folderId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
      storageBucket: "documents",
      fileSizeBytes: 845120,
      currentVersionNumber: 1,
      deletedAt: null,
      createdAt: "2026-09-15T03:15:00Z",
      metadata: { vendor: "OfficeMate", department: "Admin" },
      ocrText: null,
      ocrConfidence: null,
      aiClassification: null,
      aiSummary: null
    }
  ];
}

/**
 * Filters demo documents by the same broad search fields shown in the UI.
 */
export function searchDemoDocuments(query: string): DocumentDto[] {
  const normalizedQuery = query.trim().toLowerCase();

  if (!normalizedQuery) {
    return getDemoDocuments();
  }

  return getDemoDocuments().filter((document) => {
    const searchableText = [
      document.fileName,
      document.documentType,
      document.status,
      document.uploadedBy,
      document.ocrText ?? "",
      document.aiClassification ?? "",
      document.aiSummary ?? "",
      ...Object.keys(document.metadata),
      ...Object.values(document.metadata)
    ].join(" ").toLowerCase();

    return searchableText.includes(normalizedQuery);
  });
}

/**
 * Creates a temporary document row after a demo upload.
 */
export function createDemoUploadedDocument(input: {
  file: File;
  uploadedBy: string;
  documentType: string;
  metadata: Record<string, string>;
}): DocumentDto {
  return {
    id: crypto.randomUUID(),
    fileName: input.file.name,
    contentType: input.file.type || "application/octet-stream",
    documentType: input.documentType,
    status: "OcrQueued",
    uploadedBy: input.uploadedBy,
    storagePath: `demo://${input.file.name}`,
    folderId: null,
    storageBucket: "demo",
    fileSizeBytes: input.file.size,
    currentVersionNumber: 1,
    deletedAt: null,
    createdAt: new Date().toISOString(),
    metadata: input.metadata,
    ocrText: null,
    ocrConfidence: null,
    aiClassification: null,
    aiSummary: null
  };
}
