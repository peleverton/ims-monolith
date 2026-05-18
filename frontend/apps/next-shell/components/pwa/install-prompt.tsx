"use client";

import { useEffect, useState } from "react";

const DISMISSED_KEY = "ims-install-prompt-dismissed";

interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
}

export default function InstallPrompt() {
  const [prompt, setPrompt] = useState<BeforeInstallPromptEvent | null>(null);
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    if (typeof window === "undefined") return;
    if (localStorage.getItem(DISMISSED_KEY)) return;

    const handler = (e: Event) => {
      e.preventDefault();
      setPrompt(e as BeforeInstallPromptEvent);
      setVisible(true);
    };

    window.addEventListener("beforeinstallprompt", handler);
    return () => window.removeEventListener("beforeinstallprompt", handler);
  }, []);

  const handleInstall = async () => {
    if (!prompt) return;
    await prompt.prompt();
    const { outcome } = await prompt.userChoice;
    if (outcome === "accepted") {
      setVisible(false);
      setPrompt(null);
    }
  };

  const handleDismiss = () => {
    localStorage.setItem(DISMISSED_KEY, "1");
    setVisible(false);
    setPrompt(null);
  };

  if (!visible) return null;

  return (
    <div className="fixed bottom-0 left-0 right-0 z-50 flex items-center justify-between gap-4 bg-[#1e293b] border-t border-[#334155] px-4 py-3 text-sm text-[#f1f5f9] shadow-lg md:px-6">
      <span className="flex-1">
        📲 Instale o <strong>IMS</strong> para acesso offline e notificações em tempo real.
      </span>
      <div className="flex items-center gap-2 shrink-0">
        <button
          onClick={handleInstall}
          className="rounded-md bg-[#1e40af] px-4 py-1.5 text-sm font-semibold text-white hover:bg-[#1d4ed8] transition-colors"
        >
          Instalar
        </button>
        <button
          onClick={handleDismiss}
          className="rounded-md px-3 py-1.5 text-sm text-[#94a3b8] hover:text-[#f1f5f9] transition-colors"
          aria-label="Fechar"
        >
          ✕
        </button>
      </div>
    </div>
  );
}
