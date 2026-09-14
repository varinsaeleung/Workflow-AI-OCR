import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Line,
  LineChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from "recharts";
import { AlertTriangle, Database, Gauge, ScanText, Users, Workflow } from "lucide-react";
import type { ReactNode } from "react";
import type { EnterpriseDashboardDto } from "../../types/api";

/**
 * Props used by the enterprise dashboard component.
 */
interface EnterpriseDashboardProps {
  dashboard: EnterpriseDashboardDto;
}

/**
 * Renders enterprise dashboard charts for OCR, workflow, users, errors, performance, and storage.
 */
export function EnterpriseDashboard({ dashboard }: EnterpriseDashboardProps) {
  const ocrData = [
    { name: "Queued", value: dashboard.ocr.queued, color: "#996b00" },
    { name: "Completed", value: dashboard.ocr.completed, color: "#14766b" },
    { name: "Failed", value: dashboard.ocr.failed, color: "#b33a3a" }
  ];
  const workflowData = [
    { name: "In Progress", value: dashboard.workflow.inProgress, color: "#245da8" },
    { name: "Approved", value: dashboard.workflow.approved, color: "#287a3e" },
    { name: "Rejected", value: dashboard.workflow.rejected, color: "#b33a3a" },
    { name: "Pending", value: dashboard.workflow.pendingTasks, color: "#6f4bb7" }
  ];

  return (
    <section className="enterprise-dashboard" aria-labelledby="enterprise-dashboard-title">
      <div className="dashboard-heading">
        <div>
          <p className="eyebrow">Operational Control</p>
          <h2 id="enterprise-dashboard-title">Enterprise Dashboard</h2>
        </div>
        <span>{dashboard.performance.documentsPerDay} docs/day</span>
      </div>

      <div className="enterprise-metrics">
        <DashboardMetric title="OCR" value={`${dashboard.ocr.completionRate}%`} detail={`${dashboard.ocr.completed}/${dashboard.ocr.total} completed`} icon={ScanText} />
        <DashboardMetric title="Workflow" value={dashboard.workflow.pendingTasks.toString()} detail="pending tasks" icon={Workflow} />
        <DashboardMetric title="Users" value={dashboard.users.activeUsers.toString()} detail={`${dashboard.users.roleCount} roles`} icon={Users} />
        <DashboardMetric title="Errors" value={dashboard.errors.totalErrors.toString()} detail={`${dashboard.errors.ocrErrors} OCR / ${dashboard.errors.aiErrors} AI`} icon={AlertTriangle} />
        <DashboardMetric title="Performance" value={dashboard.performance.averageOcrConfidence.toString()} detail={`${dashboard.performance.averageProcessingSeconds}s avg`} icon={Gauge} />
        <DashboardMetric title="Storage" value={formatBytes(dashboard.storage.totalBytes)} detail={`${dashboard.storage.versionCount} versions`} icon={Database} />
      </div>

      <div className="dashboard-chart-grid">
        <ChartPanel title="OCR">
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={ocrData}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="name" />
              <YAxis allowDecimals={false} />
              <Tooltip />
              <Bar dataKey="value" radius={[4, 4, 0, 0]}>
                {ocrData.map((entry) => <Cell key={entry.name} fill={entry.color} />)}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </ChartPanel>

        <ChartPanel title="Workflow">
          <ResponsiveContainer width="100%" height={240}>
            <BarChart data={workflowData}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="name" />
              <YAxis allowDecimals={false} />
              <Tooltip />
              <Bar dataKey="value" radius={[4, 4, 0, 0]}>
                {workflowData.map((entry) => <Cell key={entry.name} fill={entry.color} />)}
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </ChartPanel>

        <ChartPanel title="Throughput">
          <ResponsiveContainer width="100%" height={240}>
            <LineChart data={dashboard.throughput}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} />
              <XAxis dataKey="label" />
              <YAxis allowDecimals={false} />
              <Tooltip />
              <Line type="monotone" dataKey="value" stroke="#193b6a" strokeWidth={3} dot={{ r: 4 }} />
            </LineChart>
          </ResponsiveContainer>
        </ChartPanel>
      </div>
    </section>
  );
}

/**
 * Renders one compact dashboard metric tile.
 */
function DashboardMetric({
  title,
  value,
  detail,
  icon: Icon
}: {
  title: string;
  value: string;
  detail: string;
  icon: typeof ScanText;
}) {
  return (
    <article className="enterprise-metric">
      <span className="enterprise-metric__icon"><Icon size={18} aria-hidden="true" /></span>
      <div>
        <p>{title}</p>
        <strong>{value}</strong>
        <span>{detail}</span>
      </div>
    </article>
  );
}

/**
 * Renders a titled chart container.
 */
function ChartPanel({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="dashboard-chart-panel" aria-label={`${title} chart`}>
      <div className="section-heading">
        <h3>{title}</h3>
      </div>
      {children}
    </section>
  );
}

/**
 * Formats byte totals for dashboard storage display.
 */
function formatBytes(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  return `${Math.round(bytes / 1024)} KB`;
}
