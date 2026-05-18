import { NextRequest, NextResponse } from "next/server";
import { pushSubscriptions } from "@/lib/push-store";

export async function POST(req: NextRequest) {
  const { endpoint } = (await req.json()) as { endpoint: string };
  if (endpoint) {
    pushSubscriptions.delete(endpoint);
  }
  return NextResponse.json({ ok: true });
}
