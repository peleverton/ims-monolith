"use client";

/**
 * notification-bell.tsx — US-039: Real-Time Notifications
 *
 * Bell icon button with unread badge. Toggles the notification panel.
 */

import { BellIcon } from "lucide-react";
import { useState } from "react";
import { useNotifications } from "@/lib/use-notifications";
import { NotificationPanel } from "./notification-panel";

export function NotificationBell() {
  const [open, setOpen] = useState(false);
  const { unreadCount } = useNotifications();

  return (
    <div className="relative">
      <button
        onClick={() => setOpen((v) => !v)}
        aria-label="Notificações"
        className="relative p-2 rounded-lg text-gray-500 hover:bg-gray-100 hover:text-gray-700 transition-colors"
      >
        <BellIcon className="w-5 h-5" />
        {unreadCount > 0 && (
          <span className="absolute top-1 right-1 flex items-center justify-center min-w-4 h-4 px-1 text-[10px] font-bold bg-red-500 text-white rounded-full leading-none">
            {unreadCount > 99 ? "99+" : unreadCount}
          </span>
        )}
      </button>

      {open && <NotificationPanel onClose={() => setOpen(false)} />}
    </div>
  );
}
