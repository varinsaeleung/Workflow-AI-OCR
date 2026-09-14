import type { Edge, Node } from "@xyflow/react";
import type { WorkflowEdgeDto, WorkflowGraphDto, WorkflowNodeDto, WorkflowNodeType } from "../../types/api";

/**
 * Carries workflow metadata stored inside each React Flow node.
 */
export interface WorkflowNodeData extends Record<string, unknown> {
  label: string;
  nodeType: WorkflowNodeType;
  configJson: string;
}

/**
 * Carries workflow metadata stored inside each React Flow edge.
 */
export interface WorkflowEdgeData extends Record<string, unknown> {
  conditionExpression: string | null;
}

/**
 * Represents a workflow designer node on the canvas.
 */
export type WorkflowCanvasNode = Node<WorkflowNodeData, "workflow">;

/**
 * Represents a workflow designer connection on the canvas.
 */
export type WorkflowCanvasEdge = Edge<WorkflowEdgeData>;

/**
 * Represents validation feedback for an imported workflow graph.
 */
export interface WorkflowGraphValidation {
  isValid: boolean;
  errors: string[];
}

const nodeLabels: Record<WorkflowNodeType, string> = {
  Start: "Start",
  Ocr: "OCR",
  Ai: "AI",
  Email: "Email",
  Folder: "Folder",
  Webhook: "Webhook",
  Condition: "Condition",
  Loop: "Loop",
  Approval: "Approval",
  End: "End"
};

/**
 * Builds the default graph used by a new workflow definition.
 */
export function buildInitialWorkflowGraph(): { nodes: WorkflowCanvasNode[]; edges: WorkflowCanvasEdge[] } {
  return {
    nodes: [
      createCanvasNode("start-1", "Start", 80, 160),
      createCanvasNode("end-1", "End", 420, 160)
    ],
    edges: [
      {
        id: "start-1__end-1",
        source: "start-1",
        target: "end-1",
        data: { conditionExpression: null }
      }
    ]
  };
}

/**
 * Converts React Flow canvas state to the backend workflow graph contract.
 */
export function serializeWorkflowJson(nodes: WorkflowCanvasNode[], edges: WorkflowCanvasEdge[]): WorkflowGraphDto {
  return {
    nodes: nodes.map((node) => toWorkflowNodeDto(node)),
    edges: edges.map((edge) => toWorkflowEdgeDto(edge))
  };
}

/**
 * Converts backend workflow graph JSON into React Flow canvas state.
 */
export function deserializeWorkflowJson(graph: WorkflowGraphDto): { nodes: WorkflowCanvasNode[]; edges: WorkflowCanvasEdge[] } {
  return {
    nodes: graph.nodes.map((node) => createCanvasNode(
      node.nodeKey,
      node.nodeType,
      node.positionX,
      node.positionY,
      node.configJson
    )),
    edges: graph.edges.map((edge) => ({
      id: `${edge.sourceNodeKey}__${edge.targetNodeKey}`,
      source: edge.sourceNodeKey,
      target: edge.targetNodeKey,
      data: { conditionExpression: edge.conditionExpression ?? null }
    }))
  };
}

/**
 * Validates imported workflow JSON before the designer accepts it.
 */
export function validateWorkflowGraph(graph: WorkflowGraphDto): WorkflowGraphValidation {
  const nodeKeys = new Set(graph.nodes.map((node) => node.nodeKey));
  const errors: string[] = [];

  for (const edge of graph.edges) {
    if (!nodeKeys.has(edge.sourceNodeKey) || !nodeKeys.has(edge.targetNodeKey)) {
      errors.push(`Edge ${edge.sourceNodeKey} -> ${edge.targetNodeKey} references a missing node.`);
    }
  }

  return {
    isValid: errors.length === 0,
    errors
  };
}

/**
 * Creates a typed canvas node from workflow metadata.
 */
export function createCanvasNode(
  nodeKey: string,
  nodeType: WorkflowNodeType,
  positionX: number,
  positionY: number,
  configJson = "{}"
): WorkflowCanvasNode {
  return {
    id: nodeKey,
    type: "workflow",
    position: { x: positionX, y: positionY },
    data: {
      label: nodeLabels[nodeType],
      nodeType,
      configJson
    }
  };
}

/**
 * Converts one canvas node into the backend node DTO.
 */
function toWorkflowNodeDto(node: WorkflowCanvasNode): WorkflowNodeDto {
  return {
    nodeKey: node.id,
    nodeType: node.data.nodeType,
    positionX: node.position.x,
    positionY: node.position.y,
    configJson: node.data.configJson
  };
}

/**
 * Converts one canvas edge into the backend edge DTO.
 */
function toWorkflowEdgeDto(edge: WorkflowCanvasEdge): WorkflowEdgeDto {
  return {
    sourceNodeKey: edge.source,
    targetNodeKey: edge.target,
    conditionExpression: edge.data?.conditionExpression ?? null
  };
}
