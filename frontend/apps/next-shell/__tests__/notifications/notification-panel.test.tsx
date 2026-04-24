/**
 * US-061: Testes unitários para NotificationPanel e NotificationBell
 */

import { render, screen, fireEvent, act } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import React from "react";

// ── Mocks ────────────────────────────────────────────────────────────────

vi.mock("@/lib/signalr-client", () => ({
  getSignalRConnection: vi.fn().mockReturnValue({ on: vi.fn(), off: vi.fn() }),
  startConnection: vi.fn(),
  stopConnection: vi.fn(),
}));

vi.mock("sonner", () => ({
  toast: { info: vi.fn(), success: vi.fn(), warning: vi.fn(), error: vi.fn() },
}));

import {
  NotificationProvider,
  type AppNotification,
} from "@/components/notifications/notification-provider";
import { NotificationPanel } from "@/components/notifications/notification-panel";
import { NotificationBell } from "@/components/notifications/notification-bell";
import { NotificationContext } from "@/components/notifications/notification-provider";

// ── Helpers ───────────────────────────────────────────────────────────────

const mockNotification = (overrides?: Partial<AppNotification>): AppNotification => ({
  id: "n1",
  title: "Estoque baixo",
  message: "Produto X está com estoque crítico",
  severity: "warning",
  timestamp: new Date("2026-04-24T10:00:00"),
  read: false,
  ...overrides,
});

const makeContextValue = (overrides = {}) => ({
  notifications: [],
  unreadCount: 0,
  markAllRead: vi.fn(),
  markRead: vi.fn(),
  clearAll: vi.fn(),
  ...overrides,
});

function renderWithContext(
  ui: React.ReactElement,
  contextValue = makeContextValue()
) {
  return render(
    <NotificationContext.Provider value={contextValue}>
      {ui}
    </NotificationContext.Provider>
  );
}

// ── NotificationPanel ─────────────────────────────────────────────────────

describe("NotificationPanel", () => {
  const onClose = vi.fn();

  beforeEach(() => {
    onClose.mockClear();
  });

  it("exibe mensagem vazia quando não há notificações", () => {
    renderWithContext(<NotificationPanel onClose={onClose} />);
    expect(screen.getByText("Nenhuma notificação")).toBeInTheDocument();
  });

  it("exibe notificações na lista", () => {
    const ctx = makeContextValue({
      notifications: [mockNotification()],
      unreadCount: 1,
    });
    renderWithContext(<NotificationPanel onClose={onClose} />, ctx);
    expect(screen.getByText("Estoque baixo")).toBeInTheDocument();
    expect(screen.getByText("Produto X está com estoque crítico")).toBeInTheDocument();
  });

  it("exibe badge com contagem de não lidas", () => {
    const ctx = makeContextValue({
      notifications: [mockNotification()],
      unreadCount: 3,
    });
    renderWithContext(<NotificationPanel onClose={onClose} />, ctx);
    expect(screen.getByText("3 novas")).toBeInTheDocument();
  });

  it("chama markRead ao clicar em uma notificação", () => {
    const markRead = vi.fn();
    const ctx = makeContextValue({
      notifications: [mockNotification()],
      unreadCount: 1,
      markRead,
    });
    renderWithContext(<NotificationPanel onClose={onClose} />, ctx);
    fireEvent.click(screen.getByText("Estoque baixo").closest("li")!);
    expect(markRead).toHaveBeenCalledWith("n1");
  });

  it("chama markAllRead ao clicar em 'Marcar todas como lidas'", () => {
    const markAllRead = vi.fn();
    const ctx = makeContextValue({
      notifications: [mockNotification()],
      unreadCount: 1,
      markAllRead,
    });
    renderWithContext(<NotificationPanel onClose={onClose} />, ctx);
    fireEvent.click(screen.getByTitle("Marcar todas como lidas"));
    expect(markAllRead).toHaveBeenCalled();
  });

  it("chama clearAll ao clicar em 'Limpar todas'", () => {
    const clearAll = vi.fn();
    const ctx = makeContextValue({
      notifications: [mockNotification()],
      unreadCount: 0,
      clearAll,
    });
    renderWithContext(<NotificationPanel onClose={onClose} />, ctx);
    fireEvent.click(screen.getByTitle("Limpar todas"));
    expect(clearAll).toHaveBeenCalled();
  });

  it("chama onClose ao clicar no botão X", () => {
    renderWithContext(<NotificationPanel onClose={onClose} />);
    // O botão X é o último da linha de ações
    const buttons = screen.getAllByRole("button");
    fireEvent.click(buttons[buttons.length - 1]);
    expect(onClose).toHaveBeenCalled();
  });

  it("não exibe botão 'marcar todas' quando não há não lidas", () => {
    const ctx = makeContextValue({
      notifications: [mockNotification({ read: true })],
      unreadCount: 0,
    });
    renderWithContext(<NotificationPanel onClose={onClose} />, ctx);
    expect(screen.queryByTitle("Marcar todas como lidas")).not.toBeInTheDocument();
  });
});

// ── NotificationBell ──────────────────────────────────────────────────────

describe("NotificationBell", () => {
  it("exibe o ícone de sino", () => {
    renderWithContext(<NotificationBell />);
    expect(screen.getByRole("button", { name: "Notificações" })).toBeInTheDocument();
  });

  it("não exibe badge quando unreadCount é 0", () => {
    renderWithContext(<NotificationBell />);
    // Badge só existe se unreadCount > 0
    expect(screen.queryByText("0")).not.toBeInTheDocument();
  });

  it("exibe badge com a contagem correta", () => {
    const ctx = makeContextValue({ unreadCount: 5 });
    renderWithContext(<NotificationBell />, ctx);
    expect(screen.getByText("5")).toBeInTheDocument();
  });

  it("exibe '99+' quando unreadCount > 99", () => {
    const ctx = makeContextValue({ unreadCount: 150 });
    renderWithContext(<NotificationBell />, ctx);
    expect(screen.getByText("99+")).toBeInTheDocument();
  });

  it("abre o painel ao clicar no sino", () => {
    const ctx = makeContextValue({ notifications: [] });
    renderWithContext(<NotificationBell />, ctx);
    fireEvent.click(screen.getByRole("button", { name: "Notificações" }));
    expect(screen.getByText("Nenhuma notificação")).toBeInTheDocument();
  });

  it("fecha o painel ao clicar no sino novamente", () => {
    const ctx = makeContextValue({ notifications: [] });
    renderWithContext(<NotificationBell />, ctx);
    const bell = screen.getByRole("button", { name: "Notificações" });
    fireEvent.click(bell); // abre
    expect(screen.getByText("Nenhuma notificação")).toBeInTheDocument();
    fireEvent.click(bell); // fecha
    expect(screen.queryByText("Nenhuma notificação")).not.toBeInTheDocument();
  });
});
