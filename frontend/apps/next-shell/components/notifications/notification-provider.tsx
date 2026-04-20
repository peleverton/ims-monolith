"use client";

/**
 * notification-provider.tsx — US-039: Real-Time Notifications
 *
 * React context that manages SignalR connection lifecycle and notification state.
 * Fires sonner toasts on incoming events.
 */

import React, {
  createContext,
  useCallback,
  useEffect,
  useRef,
  useState,
} from "react";
import { toast } from "sonner";
import { getSignalRConnection, startConnection, stopConnection } from "@/lib/signalr-client";

export type NotificationSeverity = "info" | "success" | "warning" | "error";

export interface AppNotification {
  id: string;
  title: string;
  message: string;
  severity: NotificationSeverity;
  timestamp: Date;
  read: boolean;
}

interface NotificationContextValue {
  notifications: AppNotification[];
  unreadCount: number;
  markAllRead: () => void;
  markRead: (id: string) => void;
  clearAll: () => void;
}

export const NotificationContext = createContext<NotificationContextValue | null>(null);

function severityToToast(severity: NotificationSeverity) {
  switch (severity) {
    case "success": return toast.success;
    case "warning": return toast.warning;
    case "error":   return toast.error;
    default:        return toast.info;
  }
}

export function NotificationProvider({ children }: { children: React.ReactNode }) {
  const [notifications, setNotifications] = useState<AppNotification[]>([]);
  const idRef = useRef(0);

  const addNotification = useCallback(
    (title: string, message: string, severity: NotificationSeverity = "info") => {
      const id = `notif-${Date.now()}-${idRef.current++}`;
      const notif: AppNotification = {
        id,
        title,
        message,
        severity,
        timestamp: new Date(),
        read: false,
      };
      setNotifications((prev) => [notif, ...prev].slice(0, 50));
      const showToast = severityToToast(severity);
      showToast(title, { description: message });
    },
    []
  );

  useEffect(() => {
    const conn = getSignalRConnection();

    // Hub event handlers
    conn.on("NewIssue", (payload: { issueId?: string; title: string; priority: string; reporterId?: string }) => {
      addNotification(
        "🆕 Nova Issue Criada",
        `"${payload.title}" — Prioridade: ${payload.priority}`,
        "info"
      );
    });

    conn.on("IssueUpdated", (payload: { title: string; status: string }) => {
      addNotification(
        "Issue Atualizada",
        `"${payload.title}" agora está: ${payload.status}`,
        "success"
      );
    });

    conn.on("StockAlert", (payload: { productName: string; quantity: number }) => {
      addNotification(
        "Alerta de Estoque",
        `${payload.productName} está com estoque baixo (${payload.quantity} un.)`,
        "warning"
      );
    });

    conn.on("StockCritical", (payload: { productName: string }) => {
      addNotification(
        "Estoque Crítico",
        `${payload.productName} está sem estoque!`,
        "error"
      );
    });

    conn.on("Notification", (payload: { title: string; message: string; severity?: NotificationSeverity }) => {
      addNotification(payload.title, payload.message, payload.severity ?? "info");
    });

    startConnection();

    return () => {
      conn.off("NewIssue");
      conn.off("IssueUpdated");
      conn.off("StockAlert");
      conn.off("StockCritical");
      conn.off("Notification");
      stopConnection();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const markRead = useCallback((id: string) => {
    setNotifications((prev) =>
      prev.map((n) => (n.id === id ? { ...n, read: true } : n))
    );
  }, []);

  const markAllRead = useCallback(() => {
    setNotifications((prev) => prev.map((n) => ({ ...n, read: true })));
  }, []);

  const clearAll = useCallback(() => {
    setNotifications([]);
  }, []);

  const unreadCount = notifications.filter((n) => !n.read).length;

  return (
    <NotificationContext.Provider
      value={{ notifications, unreadCount, markAllRead, markRead, clearAll }}
    >
      {children}
    </NotificationContext.Provider>
  );
}
