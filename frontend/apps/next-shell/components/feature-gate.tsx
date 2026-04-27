/**
 * US-074: FeatureGate component
 * Conditionally renders children based on a feature flag.
 */

"use client";

import { type FeatureFlags, useFeatureFlag } from "@/lib/hooks/use-feature-flags";

interface FeatureGateProps {
  flag: keyof FeatureFlags;
  children: React.ReactNode;
  fallback?: React.ReactNode;
}

export function FeatureGate({ flag, children, fallback = null }: FeatureGateProps) {
  const enabled = useFeatureFlag(flag);
  return enabled ? <>{children}</> : <>{fallback}</>;
}
