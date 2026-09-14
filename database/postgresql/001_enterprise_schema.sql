-- KM AI Workflow OCR Platform
-- Enterprise PostgreSQL schema for document management, workflow, OCR, AI, metadata, users, and permissions.

create extension if not exists pgcrypto;
create extension if not exists pg_trgm;

-- Maintains updated_at consistently for mutable tables.
create or replace function set_updated_at()
returns trigger
language plpgsql
as $$
begin
  new.updated_at = now();
  return new;
end;
$$;

comment on function set_updated_at() is 'Updates updated_at before row updates on mutable enterprise tables.';

create table if not exists departments (
  id uuid primary key default gen_random_uuid(),
  code varchar(64) not null,
  name varchar(256) not null,
  parent_department_id uuid null references departments(id) on delete restrict,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  deleted_at timestamptz null,
  constraint ck_departments_code_not_blank check (length(trim(code)) > 0),
  constraint ck_departments_name_not_blank check (length(trim(name)) > 0)
);

comment on table departments is 'Enterprise organization units used for user ownership and routing.';

create table if not exists users (
  id uuid primary key default gen_random_uuid(),
  department_id uuid null references departments(id) on delete set null,
  email varchar(320) not null,
  display_name varchar(256) not null,
  password_hash varchar(512) null,
  employee_no varchar(64) null,
  status varchar(32) not null default 'Active',
  last_login_at timestamptz null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  deleted_at timestamptz null,
  constraint ck_users_email_not_blank check (length(trim(email)) > 0),
  constraint ck_users_display_name_not_blank check (length(trim(display_name)) > 0),
  constraint ck_users_password_hash_not_blank check (password_hash is null or length(trim(password_hash)) > 0),
  constraint ck_users_status check (status in ('Active', 'Suspended', 'Deactivated'))
);

comment on table users is 'Application users synchronized from SSO or managed internally.';

create table if not exists roles (
  id uuid primary key default gen_random_uuid(),
  code varchar(64) not null,
  name varchar(128) not null,
  description text null,
  is_system_role boolean not null default false,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  deleted_at timestamptz null,
  constraint ck_roles_code_not_blank check (length(trim(code)) > 0),
  constraint ck_roles_name_not_blank check (length(trim(name)) > 0)
);

comment on table roles is 'RBAC roles used by enterprise permission checks.';

create table if not exists permissions (
  id uuid primary key default gen_random_uuid(),
  code varchar(128) not null,
  name varchar(128) not null,
  module varchar(64) not null,
  description text null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ck_permissions_code_not_blank check (length(trim(code)) > 0),
  constraint ck_permissions_module_not_blank check (length(trim(module)) > 0)
);

comment on table permissions is 'Atomic permissions used by role and document access checks.';

create table if not exists user_roles (
  user_id uuid not null references users(id) on delete cascade,
  role_id uuid not null references roles(id) on delete cascade,
  assigned_by_user_id uuid null references users(id) on delete set null,
  assigned_at timestamptz not null default now(),
  primary key (user_id, role_id)
);

comment on table user_roles is 'Many-to-many mapping between users and RBAC roles.';

create table if not exists refresh_tokens (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references users(id) on delete cascade,
  token_hash varchar(256) not null,
  expires_at timestamptz not null,
  revoked_at timestamptz null,
  replaced_by_token_hash varchar(256) null,
  created_by_ip varchar(64) not null,
  revoked_by_ip varchar(64) null,
  user_agent text not null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ck_refresh_tokens_hash_not_blank check (length(trim(token_hash)) > 0),
  constraint ck_refresh_tokens_created_ip_not_blank check (length(trim(created_by_ip)) > 0),
  constraint ck_refresh_tokens_user_agent_not_blank check (length(trim(user_agent)) > 0),
  constraint ck_refresh_tokens_revoked_after_created check (revoked_at is null or revoked_at >= created_at)
);

comment on table refresh_tokens is 'Hashed refresh tokens used for JWT session rotation and revocation.';

create table if not exists role_permissions (
  role_id uuid not null references roles(id) on delete cascade,
  permission_id uuid not null references permissions(id) on delete cascade,
  granted_at timestamptz not null default now(),
  primary key (role_id, permission_id)
);

comment on table role_permissions is 'Many-to-many mapping between roles and permissions.';

create table if not exists document_types (
  id uuid primary key default gen_random_uuid(),
  code varchar(64) not null,
  name varchar(128) not null,
  description text null,
  retention_days integer not null default 2555,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  deleted_at timestamptz null,
  constraint ck_document_types_code_not_blank check (length(trim(code)) > 0),
  constraint ck_document_types_name_not_blank check (length(trim(name)) > 0),
  constraint ck_document_types_retention_days check (retention_days > 0)
);

comment on table document_types is 'Document type master data and retention policy.';

create table if not exists document_folders (
  id uuid primary key default gen_random_uuid(),
  parent_folder_id uuid null references document_folders(id) on delete restrict,
  name varchar(256) not null,
  path varchar(2048) not null,
  created_by varchar(128) not null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  deleted_at timestamptz null,
  constraint ck_document_folders_name_not_blank check (length(trim(name)) > 0),
  constraint ck_document_folders_path_not_blank check (length(trim(path)) > 0),
  constraint ck_document_folders_created_by_not_blank check (length(trim(created_by)) > 0)
);

comment on table document_folders is 'Hierarchical folders used to organize enterprise documents.';

create table if not exists documents (
  id uuid primary key default gen_random_uuid(),
  document_type_id uuid null references document_types(id) on delete restrict,
  owner_user_id uuid null references users(id) on delete set null,
  uploaded_by_user_id uuid null references users(id) on delete set null,
  folder_id uuid null references document_folders(id) on delete set null,
  file_name varchar(260) not null,
  title varchar(512) null,
  content_type varchar(128) not null,
  storage_bucket varchar(128) null,
  storage_path varchar(1024) not null,
  document_type varchar(128) not null,
  uploaded_by varchar(128) not null,
  file_size_bytes bigint null,
  checksum_sha256 char(64) null,
  status varchar(64) not null default 'Uploaded',
  current_version_number integer not null default 1,
  search_vector tsvector generated always as (
    to_tsvector(
      'simple',
      coalesce(title, '') || ' ' || coalesce(file_name, '') || ' ' || coalesce(document_type, '')
    )
  ) stored,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  deleted_at timestamptz null,
  deleted_by varchar(128) null,
  constraint ck_documents_file_name_not_blank check (length(trim(file_name)) > 0),
  constraint ck_documents_content_type_not_blank check (length(trim(content_type)) > 0),
  constraint ck_documents_storage_path_not_blank check (length(trim(storage_path)) > 0),
  constraint ck_documents_document_type_not_blank check (length(trim(document_type)) > 0),
  constraint ck_documents_uploaded_by_not_blank check (length(trim(uploaded_by)) > 0),
  constraint ck_documents_file_size_positive check (file_size_bytes is null or file_size_bytes > 0),
  constraint ck_documents_current_version_positive check (current_version_number > 0),
  constraint ck_documents_status check (status in (
    'Uploaded',
    'OcrQueued',
    'OcrCompleted',
    'OcrFailed',
    'InWorkflow',
    'Approved',
    'Rejected',
    'Archived'
  )),
  constraint ck_documents_checksum_sha256 check (checksum_sha256 is null or checksum_sha256 ~ '^[a-fA-F0-9]{64}$')
);

comment on table documents is 'Document aggregate root used by upload, search, workflow, OCR, AI, and audit flows.';

create table if not exists document_versions (
  id uuid primary key default gen_random_uuid(),
  document_id uuid not null references documents(id) on delete cascade,
  version_number integer not null,
  file_name varchar(260) not null,
  content_type varchar(128) not null,
  storage_path varchar(1024) not null,
  file_size_bytes bigint not null,
  checksum_sha256 char(64) null,
  created_by_user_id uuid null references users(id) on delete set null,
  created_by varchar(128) not null default 'system',
  created_at timestamptz not null default now(),
  constraint ck_document_versions_number_positive check (version_number > 0),
  constraint ck_document_versions_file_size_positive check (file_size_bytes > 0),
  constraint ck_document_versions_checksum_sha256 check (checksum_sha256 is null or checksum_sha256 ~ '^[a-fA-F0-9]{64}$'),
  constraint ck_document_versions_created_by_not_blank check (length(trim(created_by)) > 0),
  constraint ux_document_versions_document_number unique (document_id, version_number)
);

comment on table document_versions is 'Immutable file versions for document lifecycle and auditability.';

create table if not exists metadata_fields (
  id uuid primary key default gen_random_uuid(),
  document_type_id uuid null references document_types(id) on delete cascade,
  field_key varchar(128) not null,
  display_name varchar(256) not null,
  data_type varchar(32) not null,
  is_required boolean not null default false,
  is_searchable boolean not null default true,
  validation_regex text null,
  sort_order integer not null default 0,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  deleted_at timestamptz null,
  constraint ck_metadata_fields_key_not_blank check (length(trim(field_key)) > 0),
  constraint ck_metadata_fields_name_not_blank check (length(trim(display_name)) > 0),
  constraint ck_metadata_fields_data_type check (data_type in ('String', 'Number', 'Date', 'Boolean', 'Json'))
);

comment on table metadata_fields is 'Configurable metadata field definitions per document type.';

create table if not exists document_metadata (
  id uuid primary key default gen_random_uuid(),
  document_id uuid not null references documents(id) on delete cascade,
  key varchar(128) not null,
  value varchar(2048) not null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ck_document_metadata_key_not_blank check (length(trim(key)) > 0),
  constraint ck_document_metadata_value_not_blank check (length(trim(value)) > 0),
  constraint ux_document_metadata_document_key unique (document_id, key)
);

comment on table document_metadata is 'Backward-compatible key-value metadata used by the starter application aggregate.';

create table if not exists document_metadata_values (
  id uuid primary key default gen_random_uuid(),
  document_id uuid not null references documents(id) on delete cascade,
  field_id uuid not null references metadata_fields(id) on delete restrict,
  value_text text null,
  value_number numeric(20, 6) null,
  value_date timestamptz null,
  value_boolean boolean null,
  value_json jsonb null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ux_document_metadata_values_document_field unique (document_id, field_id),
  constraint ck_document_metadata_values_single_type check (
    ((value_text is not null)::integer +
     (value_number is not null)::integer +
     (value_date is not null)::integer +
     (value_boolean is not null)::integer +
     (value_json is not null)::integer) <= 1
  )
);

comment on table document_metadata_values is 'Typed metadata values optimized for filtering, validation, and reporting.';

create table if not exists document_access_grants (
  id uuid primary key default gen_random_uuid(),
  document_id uuid not null references documents(id) on delete cascade,
  principal_type varchar(32) not null,
  principal_id uuid not null,
  permission_code varchar(64) not null,
  granted_by_user_id uuid null references users(id) on delete set null,
  created_at timestamptz not null default now(),
  constraint ck_document_access_principal_type check (principal_type in ('User', 'Role', 'Department')),
  constraint ck_document_access_permission check (permission_code in ('View', 'Edit', 'Approve', 'Delete', 'Share'))
);

comment on table document_access_grants is 'Document-level access grants for user, role, or department principals.';

create table if not exists ocr_jobs (
  id uuid primary key default gen_random_uuid(),
  document_id uuid not null references documents(id) on delete cascade,
  document_version_id uuid null references document_versions(id) on delete set null,
  status varchar(32) not null default 'Queued',
  attempt_count integer not null default 0,
  max_attempts integer not null default 3,
  error_message text null,
  queued_at timestamptz not null default now(),
  started_at timestamptz null,
  completed_at timestamptz null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ck_ocr_jobs_status check (status in ('Queued', 'Processing', 'Succeeded', 'Failed', 'DeadLetter')),
  constraint ck_ocr_jobs_attempt_count check (attempt_count >= 0),
  constraint ck_ocr_jobs_max_attempts check (max_attempts > 0),
  constraint ck_ocr_jobs_completed_after_started check (completed_at is null or started_at is null or completed_at >= started_at)
);

comment on table ocr_jobs is 'Asynchronous OCR job tracking for RabbitMQ workers.';

create table if not exists ocr_results (
  id uuid primary key default gen_random_uuid(),
  document_id uuid not null references documents(id) on delete cascade,
  document_version_id uuid null references document_versions(id) on delete set null,
  ocr_job_id uuid null references ocr_jobs(id) on delete set null,
  extracted_text text not null,
  confidence_score numeric(5, 4) not null,
  engine varchar(128) not null,
  language_code varchar(32) null,
  page_count integer null,
  search_vector tsvector generated always as (to_tsvector('simple', coalesce(extracted_text, ''))) stored,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ck_ocr_results_text_not_blank check (length(trim(extracted_text)) > 0),
  constraint ck_ocr_results_engine_not_blank check (length(trim(engine)) > 0),
  constraint ck_ocr_results_confidence_score check (confidence_score >= 0 and confidence_score <= 1),
  constraint ck_ocr_results_page_count_positive check (page_count is null or page_count > 0)
);

comment on table ocr_results is 'OCR text output and searchable full-text vector.';

create table if not exists ai_models (
  id uuid primary key default gen_random_uuid(),
  code varchar(64) not null,
  provider_type varchar(64) not null,
  model_name varchar(256) not null,
  model_version varchar(128) null,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  deleted_at timestamptz null,
  constraint ck_ai_models_code_not_blank check (length(trim(code)) > 0),
  constraint ck_ai_models_provider_type check (provider_type in ('KeywordAi', 'Ollama', 'Transformers')),
  constraint ck_ai_models_model_name_not_blank check (length(trim(model_name)) > 0)
);

comment on table ai_models is 'Local open-source AI model registry.';

create table if not exists ai_extractions (
  id uuid primary key default gen_random_uuid(),
  document_id uuid not null references documents(id) on delete cascade,
  document_version_id uuid null references document_versions(id) on delete set null,
  ai_model_id uuid null references ai_models(id) on delete set null,
  classification varchar(128) not null,
  summary varchar(2048) not null,
  extracted_entities_json jsonb not null default '{}'::jsonb,
  confidence_score numeric(5, 4) not null,
  engine varchar(128) not null,
  prompt_version varchar(64) null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ck_ai_extractions_classification_not_blank check (length(trim(classification)) > 0),
  constraint ck_ai_extractions_summary_not_blank check (length(trim(summary)) > 0),
  constraint ck_ai_extractions_engine_not_blank check (length(trim(engine)) > 0),
  constraint ck_ai_extractions_confidence_score check (confidence_score >= 0 and confidence_score <= 1)
);

comment on table ai_extractions is 'AI classification, summary, and entity extraction output.';

create table if not exists ai_extraction_entities (
  id uuid primary key default gen_random_uuid(),
  ai_extraction_id uuid not null references ai_extractions(id) on delete cascade,
  entity_key varchar(128) not null,
  entity_value text not null,
  confidence_score numeric(5, 4) null,
  source_page integer null,
  bounding_box jsonb null,
  created_at timestamptz not null default now(),
  constraint ck_ai_extraction_entities_key_not_blank check (length(trim(entity_key)) > 0),
  constraint ck_ai_extraction_entities_value_not_blank check (length(trim(entity_value)) > 0),
  constraint ck_ai_extraction_entities_confidence_score check (confidence_score is null or (confidence_score >= 0 and confidence_score <= 1)),
  constraint ck_ai_extraction_entities_source_page_positive check (source_page is null or source_page > 0)
);

comment on table ai_extraction_entities is 'Normalized AI entities for analytics and high-selectivity filtering.';

create table if not exists workflow_definitions (
  id uuid primary key default gen_random_uuid(),
  code varchar(128) not null,
  name varchar(256) not null,
  created_by varchar(128) not null,
  is_published boolean not null default false,
  published_version_number integer not null default 0,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  deleted_at timestamptz null,
  constraint ck_workflow_definitions_code_not_blank check (length(trim(code)) > 0),
  constraint ck_workflow_definitions_name_not_blank check (length(trim(name)) > 0),
  constraint ck_workflow_definitions_created_by_not_blank check (length(trim(created_by)) > 0),
  constraint ck_workflow_definitions_published_version check (published_version_number >= 0)
);

comment on table workflow_definitions is 'Node-based workflow definitions designed through the drag-and-drop editor.';

create table if not exists workflow_nodes (
  id uuid primary key default gen_random_uuid(),
  workflow_definition_id uuid not null references workflow_definitions(id) on delete cascade,
  node_key varchar(128) not null,
  node_type varchar(32) not null,
  position_x numeric(12, 2) not null default 0,
  position_y numeric(12, 2) not null default 0,
  config_json jsonb not null default '{}'::jsonb,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ck_workflow_nodes_key_not_blank check (length(trim(node_key)) > 0),
  constraint ck_workflow_nodes_type check (node_type in ('Start', 'Ocr', 'Ai', 'Email', 'Folder', 'Webhook', 'Condition', 'Loop', 'Approval', 'End'))
);

comment on table workflow_nodes is 'Editable nodes within a workflow definition graph.';

create table if not exists workflow_edges (
  id uuid primary key default gen_random_uuid(),
  workflow_definition_id uuid not null references workflow_definitions(id) on delete cascade,
  source_node_key varchar(128) not null,
  target_node_key varchar(128) not null,
  condition_expression varchar(1024) null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ck_workflow_edges_source_not_blank check (length(trim(source_node_key)) > 0),
  constraint ck_workflow_edges_target_not_blank check (length(trim(target_node_key)) > 0)
);

comment on table workflow_edges is 'Editable directed edges within a workflow definition graph.';

create table if not exists workflow_definition_versions (
  id uuid primary key default gen_random_uuid(),
  workflow_definition_id uuid not null references workflow_definitions(id) on delete cascade,
  version_number integer not null,
  graph_json jsonb not null,
  published_by varchar(128) not null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ck_workflow_definition_versions_number check (version_number > 0),
  constraint ck_workflow_definition_versions_published_by check (length(trim(published_by)) > 0),
  constraint ux_workflow_definition_versions_number unique (workflow_definition_id, version_number)
);

comment on table workflow_definition_versions is 'Immutable published workflow graph snapshots.';

create table if not exists workflow_templates (
  id uuid primary key default gen_random_uuid(),
  code varchar(64) not null,
  name varchar(128) not null,
  version integer not null default 1,
  is_active boolean not null default true,
  created_by_user_id uuid null references users(id) on delete set null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  deleted_at timestamptz null,
  constraint ck_workflow_templates_code_not_blank check (length(trim(code)) > 0),
  constraint ck_workflow_templates_name_not_blank check (length(trim(name)) > 0),
  constraint ck_workflow_templates_version_positive check (version > 0)
);

comment on table workflow_templates is 'Versioned workflow definitions for document routing.';

create table if not exists workflow_steps (
  id uuid primary key default gen_random_uuid(),
  workflow_template_id uuid not null references workflow_templates(id) on delete cascade,
  step_order integer not null,
  name varchar(128) not null,
  action_type varchar(32) not null,
  assignee_role_id uuid null references roles(id) on delete set null,
  sla_hours integer null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ck_workflow_steps_order_positive check (step_order > 0),
  constraint ck_workflow_steps_name_not_blank check (length(trim(name)) > 0),
  constraint ck_workflow_steps_action_type check (action_type in ('Review', 'Approve', 'Reject', 'AutoRoute')),
  constraint ck_workflow_steps_sla_hours_positive check (sla_hours is null or sla_hours > 0),
  constraint ux_workflow_steps_template_order unique (workflow_template_id, step_order)
);

comment on table workflow_steps is 'Ordered steps within a workflow template.';

create table if not exists workflow_instances (
  id uuid primary key default gen_random_uuid(),
  document_id uuid not null references documents(id) on delete cascade,
  workflow_template_id uuid null references workflow_templates(id) on delete set null,
  template_key varchar(128) not null,
  started_by varchar(128) not null,
  started_by_user_id uuid null references users(id) on delete set null,
  status varchar(32) not null default 'Running',
  completed_at timestamptz null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ck_workflow_instances_template_key_not_blank check (length(trim(template_key)) > 0),
  constraint ck_workflow_instances_started_by_not_blank check (length(trim(started_by)) > 0),
  constraint ck_workflow_instances_status check (status in ('Running', 'Completed', 'Rejected', 'Cancelled'))
);

comment on table workflow_instances is 'Runtime workflow instance attached to one document.';

create table if not exists workflow_tasks (
  id uuid primary key default gen_random_uuid(),
  workflow_instance_id uuid not null references workflow_instances(id) on delete cascade,
  workflow_step_id uuid null references workflow_steps(id) on delete set null,
  assigned_to varchar(128) not null,
  assigned_to_user_id uuid null references users(id) on delete set null,
  assigned_role_id uuid null references roles(id) on delete set null,
  name varchar(128) not null,
  status varchar(64) not null default 'Pending',
  due_at timestamptz null,
  completed_by varchar(128) null,
  completed_by_user_id uuid null references users(id) on delete set null,
  comment varchar(2048) null,
  completed_at timestamptz null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ck_workflow_tasks_assigned_to_not_blank check (length(trim(assigned_to)) > 0),
  constraint ck_workflow_tasks_name_not_blank check (length(trim(name)) > 0),
  constraint ck_workflow_tasks_status check (status in ('Pending', 'Approved', 'Rejected', 'Skipped', 'Cancelled')),
  constraint ck_workflow_tasks_completed_when_terminal check (
    (status = 'Pending' and completed_at is null)
    or (status <> 'Pending')
  )
);

comment on table workflow_tasks is 'Human or system task created inside a workflow instance.';

create table if not exists workflow_task_actions (
  id uuid primary key default gen_random_uuid(),
  workflow_task_id uuid not null references workflow_tasks(id) on delete cascade,
  actor_user_id uuid null references users(id) on delete set null,
  actor varchar(128) not null,
  action varchar(32) not null,
  comment text null,
  payload jsonb not null default '{}'::jsonb,
  created_at timestamptz not null default now(),
  constraint ck_workflow_task_actions_actor_not_blank check (length(trim(actor)) > 0),
  constraint ck_workflow_task_actions_action check (action in ('Assigned', 'Approved', 'Rejected', 'Commented', 'Reassigned', 'Skipped', 'Cancelled'))
);

comment on table workflow_task_actions is 'Immutable workflow task action history.';

create table if not exists audit_logs (
  id uuid primary key default gen_random_uuid(),
  actor varchar(128) not null,
  actor_user_id uuid null references users(id) on delete set null,
  action varchar(128) not null,
  resource_type varchar(128) not null,
  resource_id varchar(256) not null,
  trace_id varchar(128) null,
  ip_address inet null,
  user_agent text null,
  before_json jsonb null,
  after_json jsonb null,
  created_at timestamptz not null default now(),
  updated_at timestamptz null,
  constraint ck_audit_logs_actor_not_blank check (length(trim(actor)) > 0),
  constraint ck_audit_logs_action_not_blank check (length(trim(action)) > 0),
  constraint ck_audit_logs_resource_type_not_blank check (length(trim(resource_type)) > 0),
  constraint ck_audit_logs_resource_id_not_blank check (length(trim(resource_id)) > 0)
);

comment on table audit_logs is 'Enterprise audit trail for user and system actions.';

create unique index if not exists ux_departments_code_active on departments (lower(code)) where deleted_at is null;
create index if not exists ix_departments_parent on departments (parent_department_id);

create unique index if not exists ux_users_email_active on users (lower(email)) where deleted_at is null;
create unique index if not exists ux_users_employee_no_active on users (employee_no) where employee_no is not null and deleted_at is null;
create index if not exists ix_users_department_status on users (department_id, status) where deleted_at is null;
create unique index if not exists ux_refresh_tokens_token_hash on refresh_tokens (token_hash);
create index if not exists ix_refresh_tokens_user_active on refresh_tokens (user_id, expires_at desc) where revoked_at is null;
create index if not exists ix_refresh_tokens_user_created_at on refresh_tokens (user_id, created_at desc);

create unique index if not exists ux_roles_code_active on roles (lower(code)) where deleted_at is null;
create unique index if not exists ux_permissions_code on permissions (lower(code));
create index if not exists ix_permissions_module on permissions (module);
create index if not exists ix_user_roles_role_id on user_roles (role_id);
create index if not exists ix_role_permissions_permission_id on role_permissions (permission_id);

create unique index if not exists ux_document_types_code_active on document_types (lower(code)) where deleted_at is null;
create index if not exists ix_document_folders_parent_path on document_folders (parent_folder_id, path) where deleted_at is null;
create unique index if not exists ux_document_folders_parent_name_active on document_folders (coalesce(parent_folder_id, '00000000-0000-0000-0000-000000000000'::uuid), lower(name)) where deleted_at is null;
create index if not exists ix_documents_document_type_status on documents (document_type_id, status) where deleted_at is null;
create index if not exists ix_documents_folder_created_at on documents (folder_id, created_at desc) where deleted_at is null;
create index if not exists ix_documents_uploaded_by_user_created_at on documents (uploaded_by_user_id, created_at desc) where deleted_at is null;
create index if not exists ix_documents_owner_user_created_at on documents (owner_user_id, created_at desc) where deleted_at is null;
create index if not exists ix_documents_status_created_at on documents (status, created_at desc) where deleted_at is null;
create index if not exists ix_documents_created_at on documents (created_at desc);
create index if not exists ix_documents_search_vector on documents using gin (search_vector);
create index if not exists ix_documents_file_name_trgm on documents using gin (file_name gin_trgm_ops);

create index if not exists ix_document_versions_document_id on document_versions (document_id, version_number desc);
create index if not exists ix_document_metadata_document_id on document_metadata (document_id);
create index if not exists ix_document_metadata_key_value on document_metadata (key, value);
create index if not exists ix_metadata_fields_document_type on metadata_fields (document_type_id, sort_order) where deleted_at is null;
create unique index if not exists ux_metadata_fields_type_key_active on metadata_fields (document_type_id, lower(field_key)) where deleted_at is null;
create index if not exists ix_document_metadata_values_field_text on document_metadata_values using gin (value_text gin_trgm_ops) where value_text is not null;
create index if not exists ix_document_metadata_values_field_number on document_metadata_values (field_id, value_number) where value_number is not null;
create index if not exists ix_document_metadata_values_field_date on document_metadata_values (field_id, value_date) where value_date is not null;
create index if not exists ix_document_metadata_values_json on document_metadata_values using gin (value_json) where value_json is not null;
create unique index if not exists ux_document_access_grants on document_access_grants (document_id, principal_type, principal_id, permission_code);
create index if not exists ix_document_access_grants_principal on document_access_grants (principal_type, principal_id, permission_code);

create index if not exists ix_ocr_jobs_status_queued_at on ocr_jobs (status, queued_at);
create index if not exists ix_ocr_jobs_document_id on ocr_jobs (document_id);
create index if not exists ix_ocr_jobs_ready on ocr_jobs (queued_at) where status = 'Queued';
create unique index if not exists ux_ocr_results_job on ocr_results (ocr_job_id) where ocr_job_id is not null;
create index if not exists ix_ocr_results_document_id on ocr_results (document_id);
create index if not exists ix_ocr_results_search_vector on ocr_results using gin (search_vector);

create unique index if not exists ux_ai_models_code_active on ai_models (lower(code)) where deleted_at is null;
create index if not exists ix_ai_extractions_document_id on ai_extractions (document_id);
create index if not exists ix_ai_extractions_classification on ai_extractions (classification);
create index if not exists ix_ai_extractions_entities_json on ai_extractions using gin (extracted_entities_json);
create index if not exists ix_ai_extraction_entities_key_value on ai_extraction_entities (entity_key, entity_value);
create index if not exists ix_ai_extraction_entities_extraction_id on ai_extraction_entities (ai_extraction_id);

create unique index if not exists ux_workflow_definitions_code_active on workflow_definitions (lower(code)) where deleted_at is null;
create index if not exists ix_workflow_nodes_definition_id on workflow_nodes (workflow_definition_id);
create unique index if not exists ux_workflow_nodes_definition_key on workflow_nodes (workflow_definition_id, lower(node_key));
create index if not exists ix_workflow_edges_definition_id on workflow_edges (workflow_definition_id);
create index if not exists ix_workflow_definition_versions_definition on workflow_definition_versions (workflow_definition_id, version_number desc);

create unique index if not exists ux_workflow_templates_code_version_active on workflow_templates (lower(code), version) where deleted_at is null;
create index if not exists ix_workflow_steps_template_id on workflow_steps (workflow_template_id, step_order);
create index if not exists ix_workflow_instances_document_id on workflow_instances (document_id);
create index if not exists ix_workflow_instances_status on workflow_instances (status, created_at desc);
create index if not exists ix_workflow_tasks_assignee_status on workflow_tasks (assigned_to_user_id, status, created_at desc);
create index if not exists ix_workflow_tasks_assigned_to_status on workflow_tasks (assigned_to, status, created_at desc);
create index if not exists ix_workflow_tasks_pending_due_at on workflow_tasks (due_at) where status = 'Pending';
create index if not exists ix_workflow_task_actions_task_id on workflow_task_actions (workflow_task_id, created_at desc);

create index if not exists ix_audit_logs_actor_created_at on audit_logs (actor_user_id, created_at desc);
create index if not exists ix_audit_logs_resource on audit_logs (resource_type, resource_id, created_at desc);
create index if not exists ix_audit_logs_created_at on audit_logs (created_at desc);

drop trigger if exists trg_departments_updated_at on departments;
create trigger trg_departments_updated_at before update on departments for each row execute function set_updated_at();

drop trigger if exists trg_users_updated_at on users;
create trigger trg_users_updated_at before update on users for each row execute function set_updated_at();

drop trigger if exists trg_refresh_tokens_updated_at on refresh_tokens;
create trigger trg_refresh_tokens_updated_at before update on refresh_tokens for each row execute function set_updated_at();

drop trigger if exists trg_roles_updated_at on roles;
create trigger trg_roles_updated_at before update on roles for each row execute function set_updated_at();

drop trigger if exists trg_permissions_updated_at on permissions;
create trigger trg_permissions_updated_at before update on permissions for each row execute function set_updated_at();

drop trigger if exists trg_document_types_updated_at on document_types;
create trigger trg_document_types_updated_at before update on document_types for each row execute function set_updated_at();

drop trigger if exists trg_document_folders_updated_at on document_folders;
create trigger trg_document_folders_updated_at before update on document_folders for each row execute function set_updated_at();

drop trigger if exists trg_documents_updated_at on documents;
create trigger trg_documents_updated_at before update on documents for each row execute function set_updated_at();

drop trigger if exists trg_document_metadata_updated_at on document_metadata;
create trigger trg_document_metadata_updated_at before update on document_metadata for each row execute function set_updated_at();

drop trigger if exists trg_document_metadata_values_updated_at on document_metadata_values;
create trigger trg_document_metadata_values_updated_at before update on document_metadata_values for each row execute function set_updated_at();

drop trigger if exists trg_ocr_jobs_updated_at on ocr_jobs;
create trigger trg_ocr_jobs_updated_at before update on ocr_jobs for each row execute function set_updated_at();

drop trigger if exists trg_ocr_results_updated_at on ocr_results;
create trigger trg_ocr_results_updated_at before update on ocr_results for each row execute function set_updated_at();

drop trigger if exists trg_ai_models_updated_at on ai_models;
create trigger trg_ai_models_updated_at before update on ai_models for each row execute function set_updated_at();

drop trigger if exists trg_ai_extractions_updated_at on ai_extractions;
create trigger trg_ai_extractions_updated_at before update on ai_extractions for each row execute function set_updated_at();

drop trigger if exists trg_workflow_definitions_updated_at on workflow_definitions;
create trigger trg_workflow_definitions_updated_at before update on workflow_definitions for each row execute function set_updated_at();

drop trigger if exists trg_workflow_nodes_updated_at on workflow_nodes;
create trigger trg_workflow_nodes_updated_at before update on workflow_nodes for each row execute function set_updated_at();

drop trigger if exists trg_workflow_edges_updated_at on workflow_edges;
create trigger trg_workflow_edges_updated_at before update on workflow_edges for each row execute function set_updated_at();

drop trigger if exists trg_workflow_templates_updated_at on workflow_templates;
create trigger trg_workflow_templates_updated_at before update on workflow_templates for each row execute function set_updated_at();

drop trigger if exists trg_workflow_steps_updated_at on workflow_steps;
create trigger trg_workflow_steps_updated_at before update on workflow_steps for each row execute function set_updated_at();

drop trigger if exists trg_workflow_instances_updated_at on workflow_instances;
create trigger trg_workflow_instances_updated_at before update on workflow_instances for each row execute function set_updated_at();

drop trigger if exists trg_workflow_tasks_updated_at on workflow_tasks;
create trigger trg_workflow_tasks_updated_at before update on workflow_tasks for each row execute function set_updated_at();

drop trigger if exists trg_audit_logs_updated_at on audit_logs;
create trigger trg_audit_logs_updated_at before update on audit_logs for each row execute function set_updated_at();
