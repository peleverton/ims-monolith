"use client";

import { useEffect } from "react";

export default function SwRegister() {
  useEffect(() => {
    if (typeof window === "undefined" || !("serviceWorker" in navigator)) return;

    let pendingSw: ServiceWorker | null = null;

    const handleControllerChange = () => {
      window.location.reload();
    };

    navigator.serviceWorker
      .register("/sw.js", { scope: "/" })
      .then((registration) => {
        registration.addEventListener("updatefound", () => {
          const newSw = registration.installing;
          if (!newSw) return;

          newSw.addEventListener("statechange", () => {
            if (newSw.state === "installed" && navigator.serviceWorker.controller) {
              // New SW waiting — store ref for later activation
              pendingSw = newSw;
              // Auto-activate after 30s if user hasn't interacted
              setTimeout(() => {
                pendingSw?.postMessage({ type: "SKIP_WAITING" });
              }, 30_000);
            }
          });
        });
      })
      .catch((err) => {
        console.error("[SW] Registration failed:", err);
      });

    navigator.serviceWorker.addEventListener("controllerchange", handleControllerChange);

    return () => {
      navigator.serviceWorker.removeEventListener("controllerchange", handleControllerChange);
    };
  }, []);

  return null;
}
