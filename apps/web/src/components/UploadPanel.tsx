import { UploadCloud } from "lucide-react";
import { FormEvent, useState } from "react";

interface UploadPanelProps {
  defaultUploadedBy: string;
  onUpload: (input: {
    file: File;
    uploadedBy: string;
    documentType: string;
    metadata: Record<string, string>;
  }) => Promise<void>;
}

/**
 * Captures document upload input and forwards it to the parent workflow.
 */
export function UploadPanel({ onUpload, defaultUploadedBy }: UploadPanelProps) {
  const [file, setFile] = useState<File | null>(null);
  const [uploadedBy, setUploadedBy] = useState(defaultUploadedBy);
  const [documentType, setDocumentType] = useState("invoice");
  const [vendor, setVendor] = useState("Konica Minolta");
  const [isSubmitting, setSubmitting] = useState(false);

  /**
   * Submits the selected file and business metadata.
   */
  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!file) {
      return;
    }

    setSubmitting(true);

    try {
      await onUpload({
        file,
        uploadedBy,
        documentType,
        metadata: { vendor }
      });
      setFile(null);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form className="upload-panel" onSubmit={handleSubmit}>
      <div className="section-heading">
        <h2>Upload Document</h2>
      </div>

      <label>
        File
        <input type="file" onChange={(event) => setFile(event.target.files?.[0] ?? null)} />
      </label>

      <label>
        Uploaded By
        <input value={uploadedBy} onChange={(event) => setUploadedBy(event.target.value)} />
      </label>

      <label>
        Document Type
        <select value={documentType} onChange={(event) => setDocumentType(event.target.value)}>
          <option value="invoice">Invoice</option>
          <option value="contract">Contract</option>
          <option value="receipt">Receipt</option>
          <option value="memo">Memo</option>
        </select>
      </label>

      <label>
        Vendor
        <input value={vendor} onChange={(event) => setVendor(event.target.value)} />
      </label>

      <button type="submit" disabled={!file || isSubmitting} title="Upload document">
        <UploadCloud size={18} aria-hidden="true" />
        {isSubmitting ? "Uploading" : "Upload"}
      </button>
    </form>
  );
}
