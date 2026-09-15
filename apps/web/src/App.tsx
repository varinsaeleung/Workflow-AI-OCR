import { ClipboardCheck, FileStack, Hourglass, LayoutDashboard, LogOut, Search, ShieldCheck, Workflow } from "lucide-react";
import { lazy, Suspense, useEffect, useState } from "react";
import { DocumentTable } from "./components/DocumentTable";
import { LoginPanel } from "./components/LoginPanel";
import { MetricTile } from "./components/MetricTile";
import { UploadPanel } from "./components/UploadPanel";
import {
  clearStoredSession,
  getEnterpriseDashboard,
  getDashboardSummary,
  isDemoMode,
  login,
  logout,
  readStoredSession,
  searchDocuments,
  type AuthSession,
  uploadDocument
} from "./services/api";
import type { DashboardSummary, DocumentDto, EnterpriseDashboardDto } from "./types/api";

const EnterpriseDashboard = lazy(() =>
  import("./features/dashboard/EnterpriseDashboard").then((module) => ({ default: module.EnterpriseDashboard }))
);
const WorkflowDesigner = lazy(() =>
  import("./features/workflowDesigner/WorkflowDesigner").then((module) => ({ default: module.WorkflowDesigner }))
);

const emptySummary: DashboardSummary = {
  totalDocuments: 0,
  ocrQueued: 0,
  ocrCompleted: 0,
  inWorkflow: 0,
  approved: 0,
  rejected: 0
};

const emptyEnterpriseDashboard: EnterpriseDashboardDto = {
  ocr: { total: 0, queued: 0, completed: 0, failed: 0, completionRate: 0 },
  workflow: { inProgress: 0, approved: 0, rejected: 0, pendingTasks: 0 },
  users: { activeUsers: 0, lockedUsers: 0, roleCount: 0 },
  errors: { totalErrors: 0, ocrErrors: 0, aiErrors: 0 },
  performance: { averageOcrConfidence: 0, documentsPerDay: 0, averageProcessingSeconds: 0 },
  storage: { documentCount: 0, totalBytes: 0, versionCount: 0 },
  throughput: []
};

/**
 * Renders the enterprise OCR workflow dashboard.
 */
export default function App() {
  const [session, setSession] = useState<AuthSession | null>(() => readStoredSession());
  const [summary, setSummary] = useState<DashboardSummary>(emptySummary);
  const [enterpriseDashboard, setEnterpriseDashboard] = useState<EnterpriseDashboardDto>(emptyEnterpriseDashboard);
  const [documents, setDocuments] = useState<DocumentDto[]>([]);
  const [query, setQuery] = useState("");
  const [message, setMessage] = useState(isDemoMode() ? "Demo Mode" : "Ready");
  const [isLoading, setLoading] = useState(false);
  const [activeView, setActiveView] = useState<"operations" | "designer">("operations");

  /**
   * Loads dashboard and document list data from the API.
   */
  async function loadData(nextQuery = query) {
    setLoading(true);

    try {
      const [nextSummary, nextEnterpriseDashboard, nextDocuments] = await Promise.all([
        getDashboardSummary(),
        getEnterpriseDashboard(),
        searchDocuments(nextQuery)
      ]);
      setSummary(nextSummary);
      setEnterpriseDashboard(nextEnterpriseDashboard);
      setDocuments(nextDocuments);
      setMessage("Synced");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Unable to load data");
    } finally {
      setLoading(false);
    }
  }

  /**
   * Runs the initial dashboard load.
   */
  useEffect(() => {
    if (session) {
      void loadData("");
    }
  }, [session]);

  /**
   * Signs in through the API and starts the dashboard session.
   */
  async function handleLogin(credentials: { email: string; password: string }) {
    setLoading(true);
    setMessage("Signing in");

    try {
      const nextSession = await login(credentials.email, credentials.password);
      setSession(nextSession);
      setMessage("Signed in");
    } catch (error) {
      setMessage(error instanceof Error ? error.message : "Unable to sign in");
    } finally {
      setLoading(false);
    }
  }

  /**
   * Revokes the current refresh token and returns the UI to the login screen.
   */
  async function handleLogout() {
    setLoading(true);

    try {
      await logout();
    } finally {
      clearStoredSession();
      setSession(null);
      setLoading(false);
    }
  }

  /**
   * Uploads a document and refreshes dashboard state.
   */
  async function handleUpload(input: {
    file: File;
    uploadedBy: string;
    documentType: string;
    metadata: Record<string, string>;
  }) {
    await uploadDocument(input);
    await loadData(query);
  }

  /**
   * Searches documents using the current query text.
   */
  async function handleSearch() {
    await loadData(query);
  }

  if (!session) {
    return <LoginPanel isDemoMode={isDemoMode()} isLoading={isLoading} message={message} onLogin={handleLogin} />;
  }

  return (
    <main className="app-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">Enterprise OCR Operations</p>
          <h1>KM AI Workflow OCR Platform</h1>
        </div>
        <div className="topbar-actions">
          <span className="user-identity">{session.user.displayName}</span>
          <span className="sync-state">{isLoading ? "Loading" : message}</span>
          <button type="button" onClick={() => void handleLogout()} title="Sign out" aria-label="Sign out">
            <LogOut size={18} aria-hidden="true" />
          </button>
        </div>
      </header>

      <nav className="view-tabs" aria-label="Application views">
        <button
          type="button"
          className={activeView === "operations" ? "view-tab view-tab--active" : "view-tab"}
          onClick={() => setActiveView("operations")}
        >
          <LayoutDashboard size={17} aria-hidden="true" />
          Operations
        </button>
        <button
          type="button"
          className={activeView === "designer" ? "view-tab view-tab--active" : "view-tab"}
          onClick={() => setActiveView("designer")}
        >
          <Workflow size={17} aria-hidden="true" />
          Workflow Designer
        </button>
      </nav>

      {activeView === "operations" ? (
        <>
          <Suspense fallback={<section className="dashboard-loading">Loading dashboard</section>}>
            <EnterpriseDashboard dashboard={enterpriseDashboard} />
          </Suspense>

          <section className="metrics-grid">
            <MetricTile label="Total Documents" value={summary.totalDocuments} tone="blue" icon={FileStack} />
            <MetricTile label="OCR Queued" value={summary.ocrQueued} tone="amber" icon={Hourglass} />
            <MetricTile label="OCR Completed" value={summary.ocrCompleted} tone="teal" icon={ClipboardCheck} />
            <MetricTile label="In Workflow" value={summary.inWorkflow} tone="blue" icon={Workflow} />
            <MetricTile label="Approved" value={summary.approved} tone="green" icon={ShieldCheck} />
          </section>

          <section className="workbench">
            <UploadPanel onUpload={handleUpload} defaultUploadedBy={session.user.email} />
            <div className="search-panel">
              <div className="section-heading">
                <h2>Search</h2>
              </div>
              <div className="search-row">
                <input
                  value={query}
                  onChange={(event) => setQuery(event.target.value)}
                  placeholder="Search filename, OCR text, type, or metadata"
                />
                <button type="button" onClick={handleSearch} title="Search documents">
                  <Search size={18} aria-hidden="true" />
                  Search
                </button>
              </div>
              <DocumentTable documents={documents} />
            </div>
          </section>
        </>
      ) : (
        <Suspense fallback={<section className="dashboard-loading">Loading designer</section>}>
          <WorkflowDesigner />
        </Suspense>
      )}
    </main>
  );
}
