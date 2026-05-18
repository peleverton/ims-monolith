import { NextRequest, NextResponse } from "next/server";
import { pushSubscriptions } from "@/lib/push-store";

export async function POST(req: NextRequest) {
  const body = (await req.json()) as { endpoint: string; keys: { auth: string; p256dh: string } };
  if (!body.endpoint) {
    return NextResponse.json({ error: "Missing endpoint" }, { status: 400 });
  }
  pushSubscriptions.set(body.endpoint, body);
  return NextResponse.json({ ok: true }, { status: 201 });
}
