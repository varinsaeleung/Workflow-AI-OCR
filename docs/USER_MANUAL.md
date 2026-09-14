# User Manual

## Step 1: Login

Open the web application and sign in with your email and password.

## Step 2: View Dashboard

Use the Operations page to view:

- OCR queue, completed, and failed counts
- Workflow approvals and pending tasks
- Active users
- Error metrics
- Performance and storage usage

## Step 3: Upload Document

1. Select a PDF, JPG, or PNG file.
2. Enter uploader email.
3. Choose document type.
4. Add metadata key-value pairs.
5. Submit upload.

The system stores the file in MinIO, records metadata in PostgreSQL, and queues OCR through RabbitMQ.

## Step 4: Search Document

Use the Search box to find documents by:

- File name
- Document type
- OCR text
- AI summary
- AI classification
- Metadata

Search results are powered by OpenSearch and may include highlighted matches.

## Step 5: Preview And Download

Use document actions to preview or download the latest version of a document. Preview URLs are time-limited for security.

## Step 6: Manage Versions

Upload a new version when the document content changes. The system keeps version history while marking the latest version as current.

## Step 7: Workflow Designer

Open Workflow Designer to create a node-based workflow.

Supported node types:

- OCR
- AI
- Email
- Folder
- Webhook
- Condition
- Loop
- Approval

Drag nodes onto the canvas, connect them, then save JSON.

## Step 8: Load Workflow JSON

Paste existing workflow JSON into the designer and load it to continue editing.
