import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { WorkflowDesigner } from "./WorkflowDesigner";

describe("WorkflowDesigner", () => {
  /**
   * Ensures users can reach the core workflow designer actions.
   */
  it("renders palette, canvas, save action, and load action", () => {
    render(<WorkflowDesigner />);

    expect(screen.getByRole("heading", { name: "Workflow Designer" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "OCR" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Save JSON" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Load JSON" })).toBeInTheDocument();
    expect(screen.getByLabelText("Workflow canvas")).toBeInTheDocument();
  });

  /**
   * Ensures users can export the current graph as readable JSON.
   */
  it("writes serialized workflow JSON when save is clicked", () => {
    render(<WorkflowDesigner />);

    fireEvent.click(screen.getByRole("button", { name: "Save JSON" }));

    expect(screen.getByLabelText("Workflow JSON")).toHaveValue(
      JSON.stringify(
        {
          nodes: [
            {
              nodeKey: "start-1",
              nodeType: "Start",
              positionX: 80,
              positionY: 160,
              configJson: "{}"
            },
            {
              nodeKey: "end-1",
              nodeType: "End",
              positionX: 420,
              positionY: 160,
              configJson: "{}"
            }
          ],
          edges: [
            {
              sourceNodeKey: "start-1",
              targetNodeKey: "end-1",
              conditionExpression: null
            }
          ]
        },
        null,
        2
      )
    );
  });

  /**
   * Ensures users receive validation feedback for invalid imported JSON.
   */
  it("shows validation feedback when loading invalid workflow JSON", () => {
    render(<WorkflowDesigner />);

    fireEvent.change(screen.getByLabelText("Workflow JSON"), {
      target: {
        value: JSON.stringify({
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
        })
      }
    });
    fireEvent.click(screen.getByRole("button", { name: "Load JSON" }));

    expect(screen.getByText("Edge start-1 -> missing-1 references a missing node.")).toBeInTheDocument();
  });
});
