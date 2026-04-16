import { redirect } from "next/navigation";
import { getSession } from "@/lib/auth";
import { Sidebar } from "@/components/sidebar";
import { BlazorHost } from "@/components/blazor-host";
import { SessionSync } from "@/components/session-sync";
import { NotificationProvider } from "@/components/notifications/notification-provider";
import { NotificationBell } from "@/components/notifications/notification-bell";
import { Toaster } from "sonner";

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await getSession();
  if (!session) redirect("/login");

  const isAdmin = session.roles.includes("Admin");

  return (
    <NotificationProvider>
      <div className="flex h-screen overflow-hidden bg-(--bg-app)">
        <Sidebar isAdmin={isAdmin} />
        <div className="flex-1 flex flex-col overflow-hidden">
          {/* Top bar */}
          <header className="flex items-center justify-end px-6 py-3 bg-(--bg-surface) border-b border-(--border) shrink-0">
            <NotificationBell />
          </header>
          <main className="flex-1 overflow-y-auto">
            <div className="p-6 max-w-7xl mx-auto">{children}</div>
          </main>
        </div>
      </div>
      {/* Blazor WASM runtime — carregado lazy uma única vez no layout */}
      <BlazorHost />
      {/* Sincroniza logout entre abas via BroadcastChannel */}
      <SessionSync />
      <Toaster position="top-right" richColors closeButton />
    </NotificationProvider>
  );
}
