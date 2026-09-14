import { describe, expect, it } from "vitest";
import {
  buildInitialWorkflowGraph,
  deserializeWorkflowJson,
  serializeWorkflowJson,
  validateWorkflowGraph
} from "./graphJson";

describe("workflow graph JSON", () => {
  /**
   * Ensures the designer exports the exact backend graph contract.
   */
  it("serializes canvas nodes and edges to workflow JSON", () => {
    const graph = serializeWorkflowJson(
      [
        {
          id: "start-1",
          type: "workflow",
          position: { x: 80, y: 120 },
          data: { label: "Start", nodeType: "Start", configJson: "{}" }
        },
        {
          id: "ocr-1",
          type: "workflow",
          position: { x: 320, y: 120 },
          data: { label: "OCR", nodeType: "Ocr", configJson: "{\"language\":\"tha+eng\"}" }
        }
      ],
      [{ id: "edge-1", source: "start-1", target: "ocr-1", data: { conditionExpression: null } }]
    );

    expect(graph).toEqual({
      nodes: [
        {
          nodeKey: "start-1",
          nodeType: "Start",
          positionX: 80,
          positionY: 120,
          configJson: "{}"
        },
        {
          nodeKey: "ocr-1",
          nodeType: "Ocr",
          positionX: 320,
          positionY: 120,
          configJson: "{\"language\":\"tha+eng\"}"
        }
      ],
      edges: [
        {
          sourceNodeKey: "start-1",
          targetNodeKey: "ocr-1",
          conditionExpression: null
        }
      ]
    });
  });

  /**
   * Ensures loading JSON restores canvas coordinates and node metadata.
   */
  it("deserializes workflow JSON to canvas nodes and edges", () => {
    const canvas = deserializeWorkflowJson({
      nodes: [
        {
          nodeKey: "start-1",
          nodeType: "Start",
          positionX: 40,
          positionY: 90,
          configJson: "{}"
        },
        {
          nodeKey: "approval-1",
          nodeType: "Approval",
          positionX: 280,
          positionY: 90,
          configJson: "{\"approverRole\":\"Manager\"}"
        }
      ],
      edges: [
        {
          sourceNodeKey: "start-1",
          targetNodeKey: "approval-1",
          conditionExpression: "amount > 1000"
        }
      ]
    });

    expect(canvas.nodes).toEqual([
      {
        id: "start-1",
        type: "workflow",
        position: { x: 40, y: 90 },
        data: { label: "Start", nodeType: "Start", configJson: "{}" }
      },
      {
        id: "approval-1",
        type: "workflow",
        position: { x: 280, y: 90 },
        data: { label: "Approval", nodeType: "Approval", configJson: "{\"approverRole\":\"Manager\"}" }
      }
    ]);
    expect(canvas.edges).toEqual([
      {
        id: "start-1__approval-1",
        source: "start-1",
        target: "approval-1",
        data: { conditionExpression: "amount > 1000" }
      }
    ]);
  });

  /**
   * Ensures invalid imported JSON is rejected before it reaches the canvas.
   */
  it("rejects graphs whose edges reference missing nodes", () => {
    const validation = validateWorkflowGraph({
      nodes: [
        {
          nodeKey: "start-1",
          nodeType: "Start",
          positionX: 0,
          positionY: 0,
          configJson: "{}"
        }
      ],
      edges: [
        {
          sourceNodeKey: "start-1",
          targetNodeKey: "missing-1",
          conditionExpression: null
        }
      ]
    });

    expect(validation).toEqual({
      isValid: false,
      errors: ["Edge start-1 -> missing-1 references a missing node."]
    });
  });

  /**
   * Ensures a new designer opens with a usable Start and End skeleton.
   */
  it("creates an initial workflow graph with start and end nodes", () => {
    const graph = buildInitialWorkflowGraph();

    expect(graph.nodes.map((node) => node.data.nodeType)).toEqual(["Start", "End"]);
    expect(graph.edges).toEqual([
      {
        id: "start-1__end-1",
        source: "start-1",
        target: "end-1",
        data: { conditionExpression: null }
      }
    ]);
  });
});
