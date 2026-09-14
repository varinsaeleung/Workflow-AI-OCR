import {
  Background,
  Controls,
  Handle,
  MiniMap,
  Position,
  ReactFlow,
  ReactFlowProvider,
  addEdge,
  useEdgesState,
  useNodesState,
  type Connection,
  type NodeTypes
} from "@xyflow/react";
import "@xyflow/react/dist/style.css";
import {
  Bot,
  Braces,
  CheckCircle2,
  Download,
  FileText,
  FolderInput,
  GitBranch,
  Mail,
  MousePointer2,
  Repeat2,
  Upload,
  Webhook
} from "lucide-react";
import { useCallback, useMemo, useState, type DragEvent } from "react";
import type { WorkflowGraphDto, WorkflowNodeType } from "../../types/api";
import {
  buildInitialWorkflowGraph,
  createCanvasNode,
  deserializeWorkflowJson,
  serializeWorkflowJson,
  validateWorkflowGraph,
  type WorkflowCanvasEdge,
  type WorkflowCanvasNode,
  type WorkflowNodeData
} from "./graphJson";

const initialGraph = buildInitialWorkflowGraph();

const paletteItems: Array<{
  nodeType: WorkflowNodeType;
  label: string;
  icon: typeof FileText;
}> = [
  { nodeType: "Ocr", label: "OCR", icon: FileText },
  { nodeType: "Ai", label: "AI", icon: Bot },
  { nodeType: "Email", label: "Email", icon: Mail },
  { nodeType: "Folder", label: "Folder", icon: FolderInput },
  { nodeType: "Webhook", label: "Webhook", icon: Webhook },
  { nodeType: "Condition", label: "Condition", icon: GitBranch },
  { nodeType: "Loop", label: "Loop", icon: Repeat2 },
  { nodeType: "Approval", label: "Approval", icon: CheckCircle2 },
  { nodeType: "End", label: "End", icon: Braces }
];

/**
 * Renders one workflow node inside the React Flow canvas.
 */
function WorkflowNode({ data }: { data: WorkflowNodeData }) {
  return (
    <div className={`workflow-node workflow-node--${data.nodeType.toLowerCase()}`}>
      <Handle type="target" position={Position.Left} />
      <strong>{data.label}</strong>
      <span>{data.nodeType}</span>
      <Handle type="source" position={Position.Right} />
    </div>
  );
}

/**
 * Renders the enterprise workflow designer shell with React Flow provider context.
 */
export function WorkflowDesigner() {
  return (
    <ReactFlowProvider>
      <WorkflowDesignerCanvas />
    </ReactFlowProvider>
  );
}

/**
 * Renders the editable workflow canvas, palette, and JSON import/export panel.
 */
function WorkflowDesignerCanvas() {
  const [nodes, setNodes, onNodesChange] = useNodesState<WorkflowCanvasNode>(initialGraph.nodes);
  const [edges, setEdges, onEdgesChange] = useEdgesState<WorkflowCanvasEdge>(initialGraph.edges);
  const [jsonText, setJsonText] = useState(() => JSON.stringify(serializeWorkflowJson(initialGraph.nodes, initialGraph.edges), null, 2));
  const [status, setStatus] = useState("Ready");
  const nodeTypes = useMemo<NodeTypes>(() => ({ workflow: WorkflowNode }), []);

  /**
   * Adds a new connection between two workflow nodes.
   */
  const handleConnect = useCallback((connection: Connection) => {
    setEdges((currentEdges) => addEdge({ ...connection, data: { conditionExpression: null } }, currentEdges));
    setStatus("Connection added");
  }, [setEdges]);

  /**
   * Stores the selected palette node type in the drag payload.
   */
  function handleDragStart(event: DragEvent<HTMLButtonElement>, nodeType: WorkflowNodeType) {
    event.dataTransfer.setData("application/km-workflow-node", nodeType);
    event.dataTransfer.effectAllowed = "move";
  }

  /**
   * Allows workflow nodes to be dropped on the canvas.
   */
  function handleDragOver(event: DragEvent<HTMLDivElement>) {
    event.preventDefault();
    event.dataTransfer.dropEffect = "move";
  }

  /**
   * Creates a workflow node at the dropped canvas position.
   */
  function handleDrop(event: DragEvent<HTMLDivElement>) {
    event.preventDefault();
    const nodeType = event.dataTransfer.getData("application/km-workflow-node") as WorkflowNodeType;

    if (!nodeType) {
      return;
    }

    const bounds = event.currentTarget.getBoundingClientRect();
    const nodeKey = `${nodeType.toLowerCase()}-${Date.now()}`;
    const nextNode = createCanvasNode(nodeKey, nodeType, event.clientX - bounds.left, event.clientY - bounds.top);
    setNodes((currentNodes) => [...currentNodes, nextNode]);
    setStatus(`${nodeType} node added`);
  }

  /**
   * Serializes the current canvas into formatted workflow JSON.
   */
  function handleSaveJson() {
    const graph = serializeWorkflowJson(nodes, edges);
    setJsonText(JSON.stringify(graph, null, 2));
    setStatus("Workflow JSON saved");
  }

  /**
   * Loads formatted workflow JSON into the canvas after validation.
   */
  function handleLoadJson() {
    let graph: WorkflowGraphDto;

    try {
      graph = JSON.parse(jsonText) as WorkflowGraphDto;
    } catch {
      setStatus("JSON is not valid");
      return;
    }

    const validation = validateWorkflowGraph(graph);

    if (!validation.isValid) {
      setStatus(validation.errors.join(" "));
      return;
    }

    const canvas = deserializeWorkflowJson(graph);
    setNodes(canvas.nodes);
    setEdges(canvas.edges);
    setStatus("Workflow JSON loaded");
  }

  return (
    <section className="workflow-designer" aria-labelledby="workflow-designer-title">
      <div className="designer-sidebar">
        <div className="section-heading">
          <h2 id="workflow-designer-title">Workflow Designer</h2>
          <span>{status}</span>
        </div>

        <div className="palette-grid" aria-label="Workflow node palette">
          {paletteItems.map((item) => {
            const Icon = item.icon;

            return (
              <button
                key={item.nodeType}
                type="button"
                draggable
                onDragStart={(event) => handleDragStart(event, item.nodeType)}
                onClick={() => {
                  const offset = nodes.length * 28;
                  setNodes((currentNodes) => [
                    ...currentNodes,
                    createCanvasNode(`${item.nodeType.toLowerCase()}-${Date.now()}`, item.nodeType, 180 + offset, 140 + offset)
                  ]);
                  setStatus(`${item.nodeType} node added`);
                }}
                title={`Add ${item.label} node`}
              >
                <Icon size={16} aria-hidden="true" />
                {item.label}
              </button>
            );
          })}
        </div>

        <div className="designer-actions">
          <button type="button" onClick={handleSaveJson} title="Save workflow as JSON">
            <Download size={16} aria-hidden="true" />
            Save JSON
          </button>
          <button type="button" onClick={handleLoadJson} title="Load workflow from JSON">
            <Upload size={16} aria-hidden="true" />
            Load JSON
          </button>
        </div>

        <label>
          Workflow JSON
          <textarea
            aria-label="Workflow JSON"
            value={jsonText}
            onChange={(event) => setJsonText(event.target.value)}
            spellCheck={false}
          />
        </label>
      </div>

      <div className="designer-canvas" aria-label="Workflow canvas" onDragOver={handleDragOver} onDrop={handleDrop}>
        <ReactFlow
          nodes={nodes}
          edges={edges}
          nodeTypes={nodeTypes}
          onNodesChange={onNodesChange}
          onEdgesChange={onEdgesChange}
          onConnect={handleConnect}
          fitView
        >
          <MiniMap pannable zoomable />
          <Controls />
          <Background />
        </ReactFlow>
        <div className="canvas-hint">
          <MousePointer2 size={16} aria-hidden="true" />
          Drag nodes here, then connect handles
        </div>
      </div>
    </section>
  );
}
