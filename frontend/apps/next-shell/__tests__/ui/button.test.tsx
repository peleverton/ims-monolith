/**
 * US-058: Unit tests for ui/Button component.
 * Verifies rendering, variants, sizes, loading state, and disabled behavior.
 */

import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, it, expect, vi } from "vitest";
import { Button } from "@/components/ui/button";

describe("Button", () => {
  it("renders children", () => {
    render(<Button>Salvar</Button>);
    expect(screen.getByRole("button", { name: "Salvar" })).toBeInTheDocument();
  });

  it("calls onClick when clicked", async () => {
    const user = userEvent.setup();
    const handler = vi.fn();
    render(<Button onClick={handler}>Clique</Button>);
    await user.click(screen.getByRole("button"));
    expect(handler).toHaveBeenCalledOnce();
  });

  it("does not call onClick when disabled", async () => {
    const user = userEvent.setup();
    const handler = vi.fn();
    render(<Button disabled onClick={handler}>Clique</Button>);
    await user.click(screen.getByRole("button"));
    expect(handler).not.toHaveBeenCalled();
  });

  it("renders loading spinner and disables button when loading=true", () => {
    render(<Button loading>Salvar</Button>);
    const btn = screen.getByRole("button");
    expect(btn).toBeDisabled();
    // The spinner is a plain <span> inside the button (no aria-hidden attribute)
    const spinner = btn.querySelector("span");
    expect(spinner).toBeTruthy();
  });

  it("applies primary variant classes by default", () => {
    render(<Button>Primary</Button>);
    expect(screen.getByRole("button")).toHaveClass("bg-blue-600");
  });

  it("applies danger variant classes", () => {
    render(<Button variant="danger">Delete</Button>);
    expect(screen.getByRole("button")).toHaveClass("bg-red-600");
  });

  it("applies sm size classes", () => {
    render(<Button size="sm">Small</Button>);
    expect(screen.getByRole("button")).toHaveClass("px-3");
  });

  it("applies lg size classes", () => {
    render(<Button size="lg">Large</Button>);
    expect(screen.getByRole("button")).toHaveClass("px-5");
  });

  it("forwards extra HTML attributes", () => {
    render(<Button data-testid="my-btn" type="submit">Send</Button>);
    expect(screen.getByTestId("my-btn")).toHaveAttribute("type", "submit");
  });
});
