import { NextResponse } from "next/server";

/**
 * GET /api/health
 * Endpoint de healthcheck para Docker e load balancer.
 */
export function GET() {
  return NextResponse.json(
    {
      status: "ok",
      service: "ims-frontend",
      timestamp: new Date().toISOString(),
    },
    { status: 200 }
  );
}
