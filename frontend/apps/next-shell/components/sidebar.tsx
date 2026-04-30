"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { toast } from "sonner";
import {
  LayoutDashboard,
  AlertCircle,
  Package,
  BarChart3,
  LogOut,
  Menu,
  X,
  Users,
} from "lucide-react";
import { useState } from "react";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils";
import { LocaleSwitcher } from "./locale-switcher";
import { ThemeToggle } from "./theme-toggle";
import { broadcastLogout } from "@/lib/session-sync";

export function Sidebar({ isAdmin = false }: { isAdmin?: boolean }) {
  const pathname = usePathname();
  const router = useRouter();
  const [mobileOpen, setMobileOpen] = useState(false);
  const t = useTranslations("nav");
  const tCommon = useTranslations("common");
  const tAuth = useTranslations("auth.logout");

  const navItems = [
    { href: "/", label: t("dashboard"), icon: LayoutDashboard },
    { href: "/issues", label: t("issues"), icon: AlertCircle },
    { href: "/inventory", label: t("inventory"), icon: Package },
    { href: "/analytics", label: t("analytics"), icon: BarChart3 },
    ...(isAdmin
      ? [{ href: "/admin/users", label: t("users"), icon: Users }]
      : []),
  ];

  const handleLogout = async () => {
    await fetch("/api/auth/logout", { method: "POST" });
    broadcastLogout();
    toast.success(tAuth("success"));
    router.push("/login");
    router.refresh();
  };

  // eslint-disable-next-line react/no-unstable-nested-components
  const NavContent = () => (    <nav className="flex flex-col h-full">
      <div className="px-6 py-5 border-b border-slate-700">
        <div className="flex items-center gap-2">
          <LayoutDashboard size={20} className="text-blue-400" />
          <span className="font-bold text-white text-lg">IMS</span>
        </div>
        <p className="text-slate-400 text-xs mt-0.5">{tCommon("appName")}</p>
      </div>

      <ul className="flex-1 px-3 py-4 space-y-1">
        {navItems.map(({ href, label, icon: Icon }) => (
          <li key={href}>
            <Link
              href={href}
              onClick={() => setMobileOpen(false)}
              className={cn(
                "flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors",
                pathname.includes(href)
                  ? "bg-blue-600 text-white"
                  : "text-slate-300 hover:bg-slate-700 hover:text-white"
              )}
            >
              <Icon size={18} />
              {label}
            </Link>
          </li>
        ))}
      </ul>

      <div className="px-3 py-2 border-t border-slate-700 flex items-center justify-between">
        <LocaleSwitcher />
        <ThemeToggle />
      </div>

      <div className="px-3 py-4">
        <button
          onClick={handleLogout}
          className="flex items-center gap-3 w-full px-3 py-2.5 rounded-lg text-sm font-medium text-slate-300 hover:bg-slate-700 hover:text-white transition-colors"
        >
          <LogOut size={18} />
          {t("logout")}
        </button>
      </div>
    </nav>
  );

  return (
    // eslint-disable-next-line react-hooks/static-components
    <>
      {/* Desktop sidebar */}
      <aside className="hidden md:flex flex-col w-60 bg-slate-900 border-r border-slate-700 shrink-0">
        {/* eslint-disable-next-line react-hooks/static-components */}
        <NavContent />
      </aside>

      {/* Mobile top bar */}
      <div className="md:hidden flex items-center justify-between px-4 py-3 bg-slate-900 border-b border-slate-700">
        <span className="font-bold text-white">IMS</span>
        <button
          onClick={() => setMobileOpen(!mobileOpen)}
          className="text-white"
          aria-label={mobileOpen ? t("closeMenu") : t("openMenu")}
        >
          {mobileOpen ? <X size={22} /> : <Menu size={22} />}
        </button>
      </div>

      {/* Mobile drawer */}
      {mobileOpen && (
        <div className="md:hidden fixed inset-0 z-50 bg-black/60" onClick={() => setMobileOpen(false)}>
          <aside className="w-60 h-full bg-slate-900" onClick={(e) => e.stopPropagation()}>
            {/* eslint-disable-next-line react-hooks/static-components */}
            <NavContent />
          </aside>
        </div>
      )}
    </>
  );
}
