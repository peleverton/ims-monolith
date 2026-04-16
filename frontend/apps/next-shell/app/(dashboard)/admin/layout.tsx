/**
 * Admin Layout — US-040
 * Valida que o usuário tem role Admin.
 * Redireciona para /dashboard se não for admin.
 */

import { redirect } from "next/navigation";
import { getSession } from "@/lib/auth";

export const metadata = { title: "Administração" };

export default async function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const session = await getSession();
  if (!session || !session.roles.includes("Admin")) {
    redirect("/issues");
  }

  return <>{children}</>;
}
