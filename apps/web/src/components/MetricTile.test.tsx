import { FileStack } from "lucide-react";
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { MetricTile } from "./MetricTile";

describe("MetricTile", () => {
  /**
   * Ensures dashboard metric labels and values are visible to users.
   */
  it("renders label and value", () => {
    render(<MetricTile label="Total Documents" value={12} tone="blue" icon={FileStack} />);

    expect(screen.getByText("Total Documents")).toBeInTheDocument();
    expect(screen.getByText("12")).toBeInTheDocument();
  });
});
