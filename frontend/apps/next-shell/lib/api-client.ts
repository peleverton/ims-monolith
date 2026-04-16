/**
 * api-client.ts — US-037: Auth Hardening
 *
 * Fetch wrapper client-side com:
 *  - Interceptor de 401: pausa a fila, faz refresh, reexecuta requests
 *  - Mutex de refresh: garante que apenas 1 refresh ocorre por vez
 *  - Redirect para /login se o refresh falhar
 *  - Tipagem genérica de resposta
 */

type QueueEntry = {
  resolve: (value: unknown) => void;
  reject: (reason?: unknown) => void;
};

let isRefreshing = false;
let failedQueue: QueueEntry[] = [];

function processQueue(error: unknown) {
  failedQueue.forEach((entry) => {
    if (error) {
      entry.reject(error);
    } else {
      entry.resolve(undefined);
    }
  });
  failedQueue = [];
}

async function attemptRefresh(): Promise<boolean> {
  try {
    const res = await fetch("/api/auth/refresh", {
      method: "POST",
      credentials: "include",
    });
    return res.ok;
  } catch {
    return false;
  }
}

export interface ApiClientOptions extends RequestInit {
  /** Se false, não tenta refresh em 401 (ex: na própria rota de login) */
  skipRefresh?: boolean;
}

/**
 * apiFetch — wrapper client-side que usa o BFF proxy.
 * Ao receber 401, tenta refresh automático uma vez antes de redirecionar para /login.
 * Requests paralelos são enfileirados e re-executados após o refresh.
 *
 * @example
 * const data = await apiFetch<IssueDto[]>('/api/proxy/issues');
 */
export async function apiFetch<T = unknown>(
  url: string,
  options: ApiClientOptions = {}
): Promise<T> {
  const { skipRefresh = false, ...fetchOptions } = options;

  const res = await fetch(url, {
    ...fetchOptions,
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
      ...(fetchOptions.headers as Record<string, string>),
    },
  });

  // Resposta OK
  if (res.ok) {
    if (res.status === 204) return undefined as T;
    return res.json() as Promise<T>;
  }

  // Erros que não são 401 — lança imediatamente
  if (res.status !== 401 || skipRefresh) {
    const body = await res.text().catch(() => res.statusText);
    throw new ApiError(res.status, body, url);
  }

  // ── 401: tentar refresh ──────────────────────────────────────────────────
  if (isRefreshing) {
    // Outro request já está fazendo refresh — enfileira e aguarda
    return new Promise<T>((resolve, reject) => {
      failedQueue.push({
        resolve: () =>
          apiFetch<T>(url, { ...options, skipRefresh: true })
            .then(resolve)
            .catch(reject),
        reject,
      });
    });
  }

  isRefreshing = true;

  const refreshed = await attemptRefresh();

  isRefreshing = false;

  if (!refreshed) {
    processQueue(new ApiError(401, "Session expired", url));
    // Broadcast logout para outras abas e redireciona
    if (typeof window !== "undefined") {
      try {
        new BroadcastChannel("ims_session").postMessage({ type: "LOGOUT" });
      } catch {
        // BroadcastChannel não suportado em todos os ambientes
      }
      window.location.href = "/login?reason=session_expired";
    }
    throw new ApiError(401, "Session expired", url);
  }

  // Refresh bem-sucedido — processa a fila e reexecuta o request original
  processQueue(null);

  return apiFetch<T>(url, { ...options, skipRefresh: true });
}

// ── Classe de erro tipado ────────────────────────────────────────────────────

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly body: string,
    public readonly url: string
  ) {
    super(`API ${status}: ${body} [${url}]`);
    this.name = "ApiError";
  }

  get isUnauthorized() {
    return this.status === 401;
  }

  get isNotFound() {
    return this.status === 404;
  }

  get isServerError() {
    return this.status >= 500;
  }
}
