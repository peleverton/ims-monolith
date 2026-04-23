/**
 * US-058: Unit tests for analytics/ExportButton component.
 * Verifies dropdown rendering, fetch call, file download and error handling.
 */

import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { ExportButton } from "@/components/analytics/export-button";

// ── Helpers ───────────────────────────────────────────────────────────────

function mockFetchSuccess(contentType = "application/json") {
  const blob = new Blob(['{"data":"test"}'], { type: contentType });
  globalThis.fetch = vi.fn().mockResolvedValue({
    ok: true,
    headers: { get: () => contentType },
    blob: () => Promise.resolve(blob),
  } as unknown as Response);
}

function mockFetchError(status = 500, text = "Internal Server Error") {
  globalThis.fetch = vi.fn().mockResolvedValue({
    ok: false,
    status,
    text: () => Promise.resolve(text),
    statusText: text,
  } as unknown as Response);
}

// Mock URL.createObjectURL / revokeObjectURL (not available in jsdom)
beforeEach(() => {
  globalThis.URL.createObjectURL = vi.fn().mockReturnValue("blob:mock");
  globalThis.URL.revokeObjectURL = vi.fn();
});

afterEach(() => {
  vi.restoreAllMocks();
});

// ── Tests ─────────────────────────────────────────────────────────────────

describe("ExportButton", () => {
  it("renders the export button", () => {
    render(<ExportButton />);
    expect(screen.getByRole("button", { name: /exportar|export/i })).toBeInTheDocument();
  });

  it("opens the dropdown when clicked", async () => {
    const user = userEvent.setup();
    render(<ExportButton />);

    await user.click(screen.getByRole("button", { name: /exportar|export/i }));

    // The dropdown renders JSON and CSV buttons — use getAllByRole to handle duplicates from inner spans
    const buttons = screen.getAllByRole("button");
    const jsonBtn = buttons.find((b) => b.textContent?.includes("JSON"));
    const csvBtn = buttons.find((b) => b.textContent?.includes("CSV") && b.tagName === "BUTTON");
    expect(jsonBtn).toBeVisible();
    expect(csvBtn).toBeVisible();
  });

  it("closes the dropdown when clicking the backdrop overlay", async () => {
    const user = userEvent.setup();
    render(<ExportButton />);

    await user.click(screen.getByRole("button", { name: /exportar|export/i }));
    const buttons = screen.getAllByRole("button");
    expect(buttons.some((b) => b.textContent?.includes("JSON"))).toBe(true);

    // The component renders a fixed-inset backdrop div (aria-hidden) to close on outside-click
    const backdrop = document.querySelector("[aria-hidden='true']") as HTMLElement;
    expect(backdrop).toBeTruthy();
    await user.click(backdrop);

    await waitFor(() => {
      const btns = screen.queryAllByRole("button");
      expect(btns.some((b) => b.textContent?.includes("JSON"))).toBe(false);
    });
  });

  it("calls fetch with correct JSON params when JSON is selected", async () => {
    mockFetchSuccess("application/json");
    const user = userEvent.setup();
    render(<ExportButton />);

    await user.click(screen.getByRole("button", { name: /exportar|export/i }));
    await user.click(screen.getByText(/json/i));

    await waitFor(() => {
      expect(globalThis.fetch).toHaveBeenCalledWith(
        expect.stringContaining("format=json")
      );
    });
  });

  it("calls fetch with correct CSV params when CSV is selected", async () => {
    mockFetchSuccess("text/csv");
    const user = userEvent.setup();
    render(<ExportButton />);

    await user.click(screen.getByRole("button", { name: /exportar|export/i }));
    // Click the CSV button (find by textContent to avoid span ambiguity)
    const csvBtn = screen.getAllByRole("button").find(
      (b) => b.textContent?.trim().startsWith("CSV") || b.textContent?.includes("CSV")
    )!;
    await user.click(csvBtn);

    await waitFor(() => {
      expect(globalThis.fetch).toHaveBeenCalledWith(
        expect.stringContaining("format=csv")
      );
    });
  });

  it("appends module param when module prop is provided", async () => {
    mockFetchSuccess("application/json");
    const user = userEvent.setup();
    render(<ExportButton module="issues" />);

    await user.click(screen.getByRole("button", { name: /exportar|export/i }));
    await user.click(screen.getByText(/json/i));

    await waitFor(() => {
      expect(globalThis.fetch).toHaveBeenCalledWith(
        expect.stringContaining("module=issues")
      );
    });
  });

  it("shows loading state while fetching", async () => {
    // fetch never resolves during this check
    globalThis.fetch = vi.fn().mockReturnValue(new Promise(() => {}));
    const user = userEvent.setup();
    render(<ExportButton />);

    await user.click(screen.getByRole("button", { name: /exportar|export/i }));
    await user.click(screen.getByText(/json/i));

    // Button should be disabled while loading
    await waitFor(() => {
      const btn = screen.getByRole("button", { name: /exportar|export/i });
      expect(btn).toBeDisabled();
    });
  });

  it("displays error message on fetch failure", async () => {
    mockFetchError(500, "Server Error");
    const user = userEvent.setup();
    render(<ExportButton />);

    await user.click(screen.getByRole("button", { name: /exportar|export/i }));
    await user.click(screen.getByText(/json/i));

    await waitFor(() => {
      expect(screen.getByText(/erro|error/i)).toBeInTheDocument();
    });
  });
});
