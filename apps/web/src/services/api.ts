import type { DashboardSummary, DocumentDto, EnterpriseDashboardDto } from "../types/api";

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "/api/v1";
const SESSION_STORAGE_KEY = "km-ocr.auth.session";

/**
 * Represents the authenticated user returned by the API.
 */
export interface AuthenticatedUser {
  id: string;
  email: string;
  displayName: string;
}

/**
 * Represents the access and refresh token pair stored by the web client.
 */
export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  user: AuthenticatedUser;
  permissions: string[];
}

/**
 * Reads a previously authenticated session from browser storage.
 */
export function readStoredSession(): AuthSession | null {
  const rawSession = window.sessionStorage.getItem(SESSION_STORAGE_KEY);

  if (!rawSession) {
    return null;
  }

  try {
    return JSON.parse(rawSession) as AuthSession;
  } catch {
    clearStoredSession();
    return null;
  }
}

/**
 * Stores the latest token pair after login or refresh rotation.
 */
export function storeSession(session: AuthSession): void {
  window.sessionStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(session));
}

/**
 * Removes the browser session after logout or invalid refresh token response.
 */
export function clearStoredSession(): void {
  window.sessionStorage.removeItem(SESSION_STORAGE_KEY);
}

/**
 * Converts failed HTTP responses into readable errors.
 */
async function ensureOk(response: Response): Promise<Response> {
  if (response.ok) {
    return response;
  }

  const detail = await response.text();
  throw new Error(detail || `Request failed with status ${response.status}`);
}

/**
 * Requests a new token pair without recursively attaching the expired access token.
 */
async function rotateSession(refreshToken: string): Promise<AuthSession | null> {
  const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ refreshToken })
  });

  if (!response.ok) {
    return null;
  }

  return response.json() as Promise<AuthSession>;
}

let refreshPromise: Promise<AuthSession | null> | null = null;

/**
 * Sends an API request with the current access token and one refresh retry on expiry.
 */
async function request(path: string, init: RequestInit = {}, retryRefresh = true): Promise<Response> {
  const session = readStoredSession();
  const headers = new Headers(init.headers);

  if (session?.accessToken) {
    headers.set("Authorization", `Bearer ${session.accessToken}`);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers });

  if (response.status !== 401 || !retryRefresh || !session?.refreshToken) {
    return ensureOk(response);
  }

  refreshPromise ??= rotateSession(session.refreshToken).finally(() => {
    refreshPromise = null;
  });
  const refreshedSession = await refreshPromise;

  if (!refreshedSession) {
    clearStoredSession();
    return ensureOk(response);
  }

  storeSession(refreshedSession);
  return request(path, init, false);
}

/**
 * Authenticates a user and stores the returned token pair.
 */
export async function login(email: string, password: string): Promise<AuthSession> {
  const response = await ensureOk(await fetch(`${API_BASE_URL}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password })
  }));
  const session = await response.json() as AuthSession;
  storeSession(session);
  return session;
}

/**
 * Revokes the current refresh token and clears the local browser session.
 */
export async function logout(): Promise<void> {
  const session = readStoredSession();

  try {
    if (session?.refreshToken) {
      await request("/auth/logout", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: session.refreshToken })
      });
    }
  } finally {
    clearStoredSession();
  }
}

/**
 * Loads dashboard summary metrics.
 */
export async function getDashboardSummary(): Promise<DashboardSummary> {
  const response = await request("/dashboard/summary");
  return response.json() as Promise<DashboardSummary>;
}

/**
 * Loads enterprise dashboard metrics and chart data.
 */
export async function getEnterpriseDashboard(): Promise<EnterpriseDashboardDto> {
  const response = await request("/dashboard/enterprise");
  return response.json() as Promise<EnterpriseDashboardDto>;
}

/**
 * Searches documents through the backend API.
 */
export async function searchDocuments(query: string): Promise<DocumentDto[]> {
  const params = new URLSearchParams();

  if (query.trim()) {
    params.set("q", query.trim());
  }

  const suffix = params.toString() ? `?${params}` : "";
  const response = await request(`/documents${suffix}`);
  return response.json() as Promise<DocumentDto[]>;
}

/**
 * Uploads one document and queues OCR processing.
 */
export async function uploadDocument(input: {
  file: File;
  uploadedBy: string;
  documentType: string;
  metadata: Record<string, string>;
}): Promise<DocumentDto> {
  const form = new FormData();
  form.append("file", input.file);
  form.append("uploadedBy", input.uploadedBy);
  form.append("documentType", input.documentType);
  form.append("metadataJson", JSON.stringify(input.metadata));

  const response = await request("/documents", {
      method: "POST",
      body: form
    });

  return response.json() as Promise<DocumentDto>;
}
