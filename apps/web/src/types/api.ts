/**
 * Dashboard summary returned by the backend API.
 */
export interface DashboardSummary {
  totalDocuments: number;
  ocrQueued: number;
  ocrCompleted: number;
  inWorkflow: number;
  approved: number;
  rejected: number;
}

/**
 * Enterprise dashboard metrics returned by the backend API.
 */
export interface EnterpriseDashboardDto {
  ocr: {
    total: number;
    queued: number;
    completed: number;
    failed: number;
    completionRate: number;
  };
  workflow: {
    inProgress: number;
    approved: number;
    rejected: number;
    pendingTasks: number;
  };
  users: {
    activeUsers: number;
    lockedUsers: number;
    roleCount: number;
  };
  errors: {
    totalErrors: number;
    ocrErrors: number;
    aiErrors: number;
  };
  performance: {
    averageOcrConfidence: number;
    documentsPerDay: number;
    averageProcessingSeconds: number;
  };
  storage: {
    documentCount: number;
    totalBytes: number;
    versionCount: number;
  };
  throughput: Array<{ label: string; value: number }>;
}

/**
 * Document row returned by the backend API.
 */
export interface DocumentDto {
  id: string;
  fileName: string;
  contentType: string;
  documentType: string;
  status: string;
  uploadedBy: string;
  storagePath: string;
  folderId?: string | null;
  storageBucket?: string | null;
  fileSizeBytes?: number | null;
  currentVersionNumber?: number;
  deletedAt?: string | null;
  createdAt: string;
  metadata: Record<string, string>;
  ocrText?: string | null;
  ocrConfidence?: number | null;
  aiClassification?: string | null;
  aiSummary?: string | null;
}

/**
 * Workflow node type supported by the enterprise workflow engine.
 */
export type WorkflowNodeType =
  | "Start"
  | "Ocr"
  | "Ai"
  | "Email"
  | "Folder"
  | "Webhook"
  | "Condition"
  | "Loop"
  | "Approval"
  | "End";

/**
 * Workflow node JSON contract shared by the designer and backend API.
 */
export interface WorkflowNodeDto {
  nodeKey: string;
  nodeType: WorkflowNodeType;
  positionX: number;
  positionY: number;
  configJson: string;
}

/**
 * Workflow edge JSON contract shared by the designer and backend API.
 */
export interface WorkflowEdgeDto {
  sourceNodeKey: string;
  targetNodeKey: string;
  conditionExpression?: string | null;
}

/**
 * Workflow graph JSON contract used by save and load actions.
 */
export interface WorkflowGraphDto {
  nodes: WorkflowNodeDto[];
  edges: WorkflowEdgeDto[];
}

/**
 * Workflow definition returned by the backend API.
 */
export interface WorkflowDefinitionDto {
  id: string;
  code: string;
  name: string;
  isPublished: boolean;
  publishedVersionNumber?: number | null;
  graph: WorkflowGraphDto;
}
