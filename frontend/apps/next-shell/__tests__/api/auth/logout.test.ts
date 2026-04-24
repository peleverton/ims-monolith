import { describe, it, expect, vi, beforeEach } from "vitest";
import type { RequestCookie } from "next/dist/compiled/@edge-runtime/cookies";

// Helpers de mock
const makeRequest = (refreshToken?: string) => ({
  cookies: {
    get: (nameOrCookie: string | RequestCookie): RequestCookie | undefined => {
      const name =
        typeof nameOrCookie === "string" ? nameOrCookie : nameOrCookie.name;
      if (name === "ims_refresh_token" && refreshToken) {
        return { name, value: refreshToken };
      }
      return undefined;
    },
  },
});

const makeResponseMock = () => {
  const setCalls: Array<[string, string, object]> = [];
  return {
    cookies: {
      set: vi.fn((name: string, value: string, opts: object) =>
        setCalls.push([name, value, opts])
      ),
      _calls: setCalls,
    },
  };
};

const globalFetch = vi.fn();
global.fetch = globalFetch;

let jsonMock: ReturnType<typeof vi.fn>;

vi.mock("next/server", async () => {
  const mod = await vi.importActual<typeof import("next/server")>("next/server");
  jsonMock = vi.fn().mockImplementation(() => makeResponseMock());
  return {
    ...mod,
    NextResponse: { ...mod.NextResponse, json: jsonMock },
  };
});

describe("POST /api/auth/logout", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    globalFetch.mockResolvedValue({ ok: true });
    process.env.IMS_API_URL = "http://localhost:5049";
    jsonMock = vi.fn().mockImplementation(() => makeResponseMock());
  });

  it("chama o backend para revogar o refresh token quando ele existe", async () => {
    const { POST } = await import("../../../app/api/auth/logout/route");
    const req = makeRequest("test-refresh-token") as never;

    await POST(req);

    expect(globalFetch).toHaveBeenCalledWith(
      "http://localhost:5049/api/auth/logout",
      expect.objectContaining({
        method: "POST",
        body: JSON.stringify({ refreshToken: "test-refresh-token" }),
        headers: { "Content-Type": "application/json" },
      })
    );
  });

  it("não chama o backend quando o refresh token não existe", async () => {
    const { POST } = await import("../../../app/api/auth/logout/route");
    const req = makeRequest() as never;

    await POST(req);

    expect(globalFetch).not.toHaveBeenCalled();
  });

  it("ainda limpa os cookies mesmo que o backend retorne erro", async () => {
    globalFetch.mockRejectedValue(new Error("Network error"));
    const { POST } = await import("../../../app/api/auth/logout/route");
    const req = makeRequest("test-refresh-token") as never;

    // Não deve lançar exceção
    const response = await POST(req);

    expect(response.cookies.set).toHaveBeenCalledWith(
      "ims_access_token",
      "",
      expect.objectContaining({ maxAge: 0 })
    );
    expect(response.cookies.set).toHaveBeenCalledWith(
      "ims_refresh_token",
      "",
      expect.objectContaining({ maxAge: 0 })
    );
  });
});
