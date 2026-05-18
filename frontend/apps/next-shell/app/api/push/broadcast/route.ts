import { NextRequest, NextResponse } from "next/server";
import { pushSubscriptions } from "@/lib/push-store";
import webPush from "web-push";

const VAPID_PUBLIC_KEY = process.env.NEXT_PUBLIC_VAPID_PUBLIC_KEY ?? "";
const VAPID_PRIVATE_KEY = process.env.VAPID_PRIVATE_KEY ?? "";
const VAPID_SUBJECT = process.env.VAPID_SUBJECT ?? "mailto:admin@ims.local";
const PUSH_SECRET = process.env.PUSH_SECRET ?? "";

if (VAPID_PUBLIC_KEY && VAPID_PRIVATE_KEY) {
  webPush.setVapidDetails(VAPID_SUBJECT, VAPID_PUBLIC_KEY, VAPID_PRIVATE_KEY);
}

interface BroadcastPayload {
  title: string;
  body: string;
  url?: string;
  tag?: string;
  requireInteraction?: boolean;
}

export async function POST(req: NextRequest) {
  const secret = req.headers.get("x-push-secret");
  if (PUSH_SECRET && secret !== PUSH_SECRET) {
    return NextResponse.json({ error: "Unauthorized" }, { status: 401 });
  }

  if (!VAPID_PUBLIC_KEY || !VAPID_PRIVATE_KEY) {
    // VAPID not configured — skip silently
    return NextResponse.json({ ok: true, skipped: true });
  }

  const payload = (await req.json()) as BroadcastPayload;
  const message = JSON.stringify(payload);

  const results = await Promise.allSettled(
    [...pushSubscriptions.values()].map((sub) =>
      webPush.sendNotification(
        { endpoint: sub.endpoint, keys: sub.keys },
        message
      ).catch((err: { statusCode?: number }) => {
        // Remove expired/invalid subscriptions
        if (err.statusCode === 410 || err.statusCode === 404) {
          pushSubscriptions.delete(sub.endpoint);
        }
        throw err;
      })
    )
  );

  const sent = results.filter((r) => r.status === "fulfilled").length;
  const failed = results.filter((r) => r.status === "rejected").length;

  return NextResponse.json({ ok: true, sent, failed });
}
