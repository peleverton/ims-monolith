/**
 * US-062: Testes unitários para componentes Kanban
 */

import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import React from "react";
import type { IssueDto } from "@/lib/types";

// ── Mocks dnd-kit ─────────────────────────────────────────────────────────

vi.mock("@dnd-kit/core", () => ({
  DndContext: ({ children }: { children: React.ReactNode }) => <div data-testid="dnd-context">{children}</div>,
  DragOverlay: ({ children }: { children: React.ReactNode }) => <div data-testid="drag-overlay">{children}</div>,
  PointerSensor: vi.fn(),
  KeyboardSensor: vi.fn(),
  useSensor: vi.fn(),
  useSensors: vi.fn(() => []),
  closestCorners: vi.fn(),
}));

vi.mock("@dnd-kit/sortable", () => ({
  SortableContext: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
  verticalListSortingStrategy: vi.fn(),
  sortableKeyboardCoordinates: vi.fn(),
  useSortable: vi.fn(() => ({
    attributes: {},
    listeners: {},
    setNodeRef: vi.fn(),
    transform: null,
    transition: null,
    isDragging: false,
  })),
}));

vi.mock("@dnd-kit/utilities", () => ({
  CSS: { Transform: { toString: vi.fn(() => undefined) } },
}));

vi.mock("@dnd-kit/core", async () => {
  const actual = await vi.importActual<typeof import("@dnd-kit/core")>("@dnd-kit/core");
  return {
    ...actual,
    DndContext: ({ children, onDragEnd }: { children: React.ReactNode; onDragEnd?: (e: unknown) => void }) => (
      <div data-testid="dnd-context" onClick={() => onDragEnd?.({ active: { id: "i1" }, over: { id: "Resolved" } })}>
        {children}
      </div>
    ),
    DragOverlay: ({ children }: { children: React.ReactNode }) => <div data-testid="drag-overlay">{children}</div>,
    useSensor: vi.fn(),
    useSensors: vi.fn(() => []),
    closestCorners: vi.fn(),
    PointerSensor: vi.fn(),
    KeyboardSensor: vi.fn(),
  };
});

// ── Helpers ───────────────────────────────────────────────────────────────

const makeIssue = (overrides?: Partial<IssueDto>): IssueDto => ({
  id: "i1",
  title: "Bug crítico no login",
  description: "Descrição do bug",
  status: "Open",
  priority: "High",
  assigneeName: "Neo",
  createdAt: "2026-04-01T10:00:00Z",
  updatedAt: "2026-04-01T10:00:00Z",
  tags: [],
  commentsCount: 0,
  ...overrides,
});

// ── KanbanCard ────────────────────────────────────────────────────────────

import { KanbanCard } from "@/components/issues/kanban-card";

describe("KanbanCard", () => {
  it("exibe o título da issue", () => {
    render(<KanbanCard issue={makeIssue()} />);
    expect(screen.getByText("Bug crítico no login")).toBeInTheDocument();
  });

  it("exibe o nome do responsável", () => {
    render(<KanbanCard issue={makeIssue()} />);
    expect(screen.getByText("👤 Neo")).toBeInTheDocument();
  });

  it("não exibe responsável quando não há assigneeName", () => {
    render(<KanbanCard issue={makeIssue({ assigneeName: undefined })} />);
    expect(screen.queryByText(/👤/)).not.toBeInTheDocument();
  });

  it("exibe link para detalhe da issue", () => {
    render(<KanbanCard issue={makeIssue()} />);
    const link = screen.getByRole("link", { name: /ver issue/i });
    expect(link).toHaveAttribute("href", "/issues/i1");
  });

  it("exibe badge de prioridade", () => {
    render(<KanbanCard issue={makeIssue({ priority: "Critical" })} />);
    // PriorityBadge renderiza o texto da prioridade
    expect(screen.getByText(/critical/i)).toBeInTheDocument();
  });

  it("aplica classe de overlay quando isOverlay=true", () => {
    const { container } = render(<KanbanCard issue={makeIssue()} isOverlay />);
    expect(container.firstChild).toHaveClass("rotate-1");
  });
});

// ── KanbanColumn ──────────────────────────────────────────────────────────

import { KanbanColumn } from "@/components/issues/kanban-column";

vi.mock("@dnd-kit/core", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@dnd-kit/core")>();
  return {
    ...actual,
    useDroppable: vi.fn(() => ({ setNodeRef: vi.fn(), isOver: false })),
    DndContext: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
    DragOverlay: ({ children }: { children: React.ReactNode }) => <div>{children}</div>,
    useSensor: vi.fn(),
    useSensors: vi.fn(() => []),
    closestCorners: vi.fn(),
    PointerSensor: vi.fn(),
    KeyboardSensor: vi.fn(),
  };
});

describe("KanbanColumn", () => {
  it("exibe o label e contagem", () => {
    render(
      <KanbanColumn id="Open" label="Aberto" colorClass="border-t-blue-500" count={3}>
        <div>card</div>
      </KanbanColumn>
    );
    expect(screen.getByText("Aberto")).toBeInTheDocument();
    expect(screen.getByText("3")).toBeInTheDocument();
  });

  it("exibe os filhos", () => {
    render(
      <KanbanColumn id="Open" label="Aberto" colorClass="border-t-blue-500" count={0}>
        <div data-testid="child-card">Card filho</div>
      </KanbanColumn>
    );
    expect(screen.getByTestId("child-card")).toBeInTheDocument();
  });
});

// ── IssuesViewToggle ──────────────────────────────────────────────────────

import { IssuesViewToggle } from "@/components/issues/issues-view-toggle";

const mockPush = vi.fn();
vi.mock("next/navigation", () => ({
  useRouter: () => ({ push: mockPush }),
  useSearchParams: () => new URLSearchParams("view=list"),
}));

describe("IssuesViewToggle", () => {
  beforeEach(() => mockPush.mockClear());

  it("renderiza os botões Lista e Kanban", () => {
    render(<IssuesViewToggle />);
    expect(screen.getByRole("button", { name: /lista/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /kanban/i })).toBeInTheDocument();
  });

  it("botão Lista está pressionado quando view=list", () => {
    render(<IssuesViewToggle />);
    expect(screen.getByRole("button", { name: /lista/i })).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByRole("button", { name: /kanban/i })).toHaveAttribute("aria-pressed", "false");
  });

  it("navega para ?view=kanban ao clicar em Kanban", () => {
    render(<IssuesViewToggle />);
    fireEvent.click(screen.getByRole("button", { name: /kanban/i }));
    expect(mockPush).toHaveBeenCalledWith(expect.stringContaining("view=kanban"));
  });

  it("navega para ?view=list ao clicar em Lista", () => {
    render(<IssuesViewToggle />);
    fireEvent.click(screen.getByRole("button", { name: /lista/i }));
    expect(mockPush).toHaveBeenCalledWith(expect.stringContaining("view=list"));
  });
});
