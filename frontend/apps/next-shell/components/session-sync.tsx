'use client';

import { useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { initSessionSync } from '@/lib/session-sync';

/**
 * SessionSync — monta o listener de BroadcastChannel para logout cross-tab.
 * Deve ser colocado no layout do dashboard (client component leaf).
 */
export function SessionSync() {
  const router = useRouter();

  useEffect(() => {
    const cleanup = initSessionSync(() => {
      router.push('/login?reason=session_expired');
    });
    return cleanup;
  }, [router]);

  return null;
}
