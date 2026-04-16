import { redirect } from "next/navigation";
import { getSession } from "@/lib/auth";
import { Sidebar } from "@/components/sidebar";
import { BlazorHost } from "@/components/blazor-host";
import { SessionSync } from "@/components/session-sync";

export default async function DashboardLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await getSession();
  if (!session) redirect("/login");

  return (
    <div className="flex h-screen overflow-hidden bg-gray-100">
      <Sidebar />
      <main className="flex-1 overflow-y-auto">
        <div className="p-6 max-w-7xl mx-auto">{children}</div>
      </main>
      {/* Blazor WASM runtime — carregado lazy uma única vez no layout */}
      <BlazorHost />
      {/* Sincroniza logout entre abas via BroadcastChannel */}
      <SessionSync />
    </div>
  );
}
