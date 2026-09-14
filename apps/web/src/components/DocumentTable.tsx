import { FileText } from "lucide-react";
import type { DocumentDto } from "../types/api";

interface DocumentTableProps {
  documents: DocumentDto[];
}

/**
 * Displays document search results in a scan-friendly table.
 */
export function DocumentTable({ documents }: DocumentTableProps) {
  return (
    <section className="table-panel">
      <div className="section-heading">
        <h2>Documents</h2>
        <span>{documents.length} records</span>
      </div>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Document</th>
              <th>Type</th>
              <th>Status</th>
              <th>AI</th>
              <th>Uploaded By</th>
              <th>Metadata</th>
            </tr>
          </thead>
          <tbody>
            {documents.map((document) => (
              <tr key={document.id}>
                <td>
                  <span className="document-name">
                    <FileText size={16} aria-hidden="true" />
                    {document.fileName}
                  </span>
                </td>
                <td>{document.documentType}</td>
                <td>
                  <span className="status-pill">{document.status}</span>
                </td>
                <td>{document.aiClassification ?? "-"}</td>
                <td>{document.uploadedBy}</td>
                <td>{formatMetadata(document.metadata)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

/**
 * Formats metadata for compact table display.
 */
function formatMetadata(metadata: Record<string, string>): string {
  const entries = Object.entries(metadata);

  if (entries.length === 0) {
    return "-";
  }

  return entries.map(([key, value]) => `${key}: ${value}`).join(", ");
}
