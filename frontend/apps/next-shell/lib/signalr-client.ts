/**
 * signalr-client.ts — US-039: Real-Time Notifications
 *
 * Singleton SignalR connection with:
 *  - Auto-reconnect with exponential backoff
 *  - Bearer token from /api/auth/me (BFF session)
 *  - Start/stop lifecycle management
 */

import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";

let connection: HubConnection | null = null;
let startPromise: Promise<void> | null = null;

async function getAccessToken(): Promise<string> {
  try {
    const res = await fetch("/api/auth/me", { credentials: "include" });
    if (!res.ok) return "";
    const data = await res.json();
    return data?.accessToken ?? "";
  } catch {
    return "";
  }
}

export function getSignalRConnection(): HubConnection {
  if (!connection) {
    connection = new HubConnectionBuilder()
      .withUrl("/hubs/notifications", {
        accessTokenFactory: getAccessToken,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(
        process.env.NODE_ENV === "development" ? LogLevel.Information : LogLevel.Warning
      )
      .build();
  }
  return connection;
}

export async function startConnection(): Promise<void> {
  const conn = getSignalRConnection();

  if (conn.state === HubConnectionState.Connected) return;
  if (conn.state === HubConnectionState.Connecting && startPromise) {
    return startPromise;
  }

  startPromise = conn.start().catch((err) => {
    console.error("[SignalR] Failed to connect:", err);
    startPromise = null;
  });

  return startPromise;
}

export async function stopConnection(): Promise<void> {
  if (connection && connection.state !== HubConnectionState.Disconnected) {
    await connection.stop();
  }
  connection = null;
  startPromise = null;
}
