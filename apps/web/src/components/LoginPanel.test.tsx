import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { LoginPanel } from "./LoginPanel";

describe("LoginPanel", () => {
  it("submits the entered email and password", async () => {
    const onLogin = vi.fn().mockResolvedValue(undefined);

    render(<LoginPanel isLoading={false} message="Ready" onLogin={onLogin} />);

    fireEvent.change(screen.getByLabelText("Email"), { target: { value: "admin@km.local" } });
    fireEvent.change(screen.getByLabelText("Password"), { target: { value: "Pass@123" } });
    fireEvent.click(screen.getByRole("button", { name: "Sign in" }));

    await waitFor(() => {
      expect(onLogin).toHaveBeenCalledWith({ email: "admin@km.local", password: "Pass@123" });
    });
  });

  it("prefills demo credentials when demo mode is enabled", () => {
    const onLogin = vi.fn().mockResolvedValue(undefined);

    render(<LoginPanel isDemoMode={true} isLoading={false} message="Ready" onLogin={onLogin} />);

    expect(screen.getByLabelText("Email")).toHaveValue("admin@km.local");
    expect(screen.getByLabelText("Password")).toHaveValue("ChangeMe!2026");
    expect(screen.getByText("Demo Mode")).toBeInTheDocument();
  });
});
