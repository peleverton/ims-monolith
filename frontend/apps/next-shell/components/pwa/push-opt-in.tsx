"use client";

import { useState } from "react";
import { subscribeToPush, unsubscribeFromPush } from "@/lib/push-notifications";

type PushState = "idle" | "loading" | "subscribed" | "denied";

export default function PushOptIn() {
  const [state, setState] = useState<PushState>("idle");

  const handleSubscribe = async () => {
    setState("loading");
    try {
      const sub = await subscribeToPush();
      setState(sub ? "subscribed" : "denied");
    } catch {
      setState("denied");
    }
  };

  const handleUnsubscribe = async () => {
    setState("loading");
    try {
      await unsubscribeFromPush();
      setState("idle");
    } catch {
      setState("subscribed");
    }
  };

  if (state === "subscribed") {
    return (
      <button
        onClick={handleUnsubscribe}
        className="flex items-center gap-1.5 rounded-md bg-green-800/30 px-3 py-1.5 text-xs font-medium text-green-400 hover:bg-green-800/50 transition-colors"
        title="Clique para cancelar notificações push"
      >
        🔔 Notificações ativas
      </button>
    );
  }

  if (state === "denied") {
    return (
      <span className="flex items-center gap-1.5 rounded-md bg-red-900/20 px-3 py-1.5 text-xs text-red-400">
        🔕 Notificações bloqueadas
      </span>
    );
  }

  return (
    <button
      onClick={handleSubscribe}
      disabled={state === "loading"}
      className="flex items-center gap-1.5 rounded-md bg-[#1e293b] border border-[#334155] px-3 py-1.5 text-xs font-medium text-[#94a3b8] hover:text-[#f1f5f9] hover:border-[#475569] transition-colors disabled:opacity-50"
    >
      {state === "loading" ? "⏳ Aguarde..." : "🔔 Ativar notificações"}
    </button>
  );
}
