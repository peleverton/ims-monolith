"use client";

/**
 * notification-panel.tsx — US-039: Real-Time Notifications
 *
 * Dropdown panel listing notifications with mark-all-read and clear actions.
 */

import { useEffect, useRef } from "react";
import { XIcon, CheckCheckIcon, Trash2Icon } from "lucide-react";
import { useNotifications } from "@/lib/use-notifications";
import type { AppNotification } from "./notification-provider";

const severityClasses: Record<string, string> = {
  info: "border-blue-400 bg-blue-50",
  success: "border-green-400 bg-green-50",
  warning: "border-yellow-400 bg-yellow-50",
  error: "border-red-400 bg-red-50",
};

const severityDot: Record<string, string> = {
  info: "bg-blue-500",
  success: "bg-green-500",
  warning: "bg-yellow-500",
  error: "bg-red-500",
};

function formatTime(date: Date) {
  return date.toLocaleTimeString("pt-BR", { hour: "2-digit", minute: "2-digit" });
}

interface NotificationPanelProps {
  onClose: () => void;
}

export function NotificationPanel({ onClose }: NotificationPanelProps) {
  const { notifications, unreadCount, markAllRead, markRead, clearAll } = useNotifications();
  const panelRef = useRef<HTMLDivElement>(null);

  // Close on outside click
  useEffect(() => {
    function handler(e: MouseEvent) {
      if (panelRef.current && !panelRef.current.contains(e.target as Node)) {
        onClose();
      }
    }
    document.addEventListener("mousedown", handler);
    return () => document.removeEventListener("mousedown", handler);
  }, [onClose]);

  return (
    <div
      ref={panelRef}
      className="absolute right-0 top-full mt-2 w-80 bg-white rounded-xl shadow-lg border border-gray-200 z-50 flex flex-col max-h-120"
    >
      {/* Header */}
      <div className="flex items-center justify-between px-4 py-3 border-b border-gray-100">
        <h3 className="text-sm font-semibold text-gray-800">
          Notificações
          {unreadCount > 0 && (
            <span className="ml-2 text-xs font-medium text-blue-600">
              {unreadCount} nova{unreadCount !== 1 ? "s" : ""}
            </span>
          )}
        </h3>
        <div className="flex items-center gap-1">
          {unreadCount > 0 && (
            <button
              onClick={markAllRead}
              title="Marcar todas como lidas"
              className="p-1 rounded text-gray-400 hover:text-green-600 hover:bg-green-50 transition-colors"
            >
              <CheckCheckIcon className="w-4 h-4" />
            </button>
          )}
          {notifications.length > 0 && (
            <button
              onClick={clearAll}
              title="Limpar todas"
              className="p-1 rounded text-gray-400 hover:text-red-600 hover:bg-red-50 transition-colors"
            >
              <Trash2Icon className="w-4 h-4" />
            </button>
          )}
          <button
            onClick={onClose}
            className="p-1 rounded text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors"
          >
            <XIcon className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* List */}
      <div className="overflow-y-auto flex-1">
        {notifications.length === 0 ? (
          <div className="px-4 py-8 text-center text-sm text-gray-400">
            Nenhuma notificação
          </div>
        ) : (
          <ul>
            {notifications.map((n: AppNotification) => (
              <li
                key={n.id}
                onClick={() => markRead(n.id)}
                className={`
                  flex gap-3 px-4 py-3 border-l-4 cursor-pointer transition-opacity
                  ${severityClasses[n.severity] ?? "border-gray-300 bg-white"}
                  ${n.read ? "opacity-60" : ""}
                  hover:brightness-95
                `}
              >
                <span
                  className={`mt-1.5 h-2 w-2 shrink-0 rounded-full ${severityDot[n.severity] ?? "bg-gray-400"}`}
                />
                <div className="flex-1 min-w-0">
                  <p className="text-xs font-semibold text-gray-800 truncate">{n.title}</p>
                  <p className="text-xs text-gray-600 mt-0.5 line-clamp-2">{n.message}</p>
                  <p className="text-[10px] text-gray-400 mt-1">{formatTime(n.timestamp)}</p>
                </div>
                {!n.read && (
                  <span className="mt-1.5 h-2 w-2 shrink-0 rounded-full bg-blue-500 self-start" />
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
