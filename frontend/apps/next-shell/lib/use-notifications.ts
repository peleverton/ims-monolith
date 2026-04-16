/**
 * use-notifications.ts — US-039: Real-Time Notifications
 *
 * Custom hook to access the notifications context.
 */

import { useContext } from "react";
import { NotificationContext } from "@/components/notifications/notification-provider";

export function useNotifications() {
  const ctx = useContext(NotificationContext);
  if (!ctx) {
    throw new Error("useNotifications must be used within NotificationProvider");
  }
  return ctx;
}
