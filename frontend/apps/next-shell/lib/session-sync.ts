/**
 * session-sync.ts — US-037
 *
 * Sincroniza eventos de sessão entre abas via BroadcastChannel.
 * - Logout em uma aba → todas as outras são redirecionadas para /login
 * - Session expired → todas redirecionam
 */

export const SESSION_CHANNEL = "ims_session";

export type SessionMessage =
  | { type: "LOGOUT" }
  | { type: "SESSION_EXPIRED" }
  | { type: "LOGIN" };

/**
 * Inicia o listener de sincronização de sessão.
 * Deve ser chamado uma vez no lado cliente (ex: layout do dashboard).
 * Retorna função de cleanup para remover o listener.
 */
export function initSessionSync(
  onLogout: () => void
): () => void {
  if (typeof window === "undefined") return () => {};

  let channel: BroadcastChannel;
  try {
    channel = new BroadcastChannel(SESSION_CHANNEL);
  } catch {
    // BroadcastChannel não disponível (ex: iframe sandbox)
    return () => {};
  }

  const handler = (event: MessageEvent<SessionMessage>) => {
    if (event.data.type === "LOGOUT" || event.data.type === "SESSION_EXPIRED") {
      onLogout();
    }
  };

  channel.addEventListener("message", handler);

  return () => {
    channel.removeEventListener("message", handler);
    channel.close();
  };
}

/**
 * Envia broadcast de logout para todas as abas.
 */
export function broadcastLogout(): void {
  if (typeof window === "undefined") return;
  try {
    const channel = new BroadcastChannel(SESSION_CHANNEL);
    channel.postMessage({ type: "LOGOUT" } satisfies SessionMessage);
    channel.close();
  } catch {
    // ignorar se não suportado
  }
}
