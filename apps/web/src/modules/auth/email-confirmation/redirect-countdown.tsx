"use client";

import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";

export function RedirectCountdown({ href, seconds }: { href: string; seconds: number }) {
  const router = useRouter();
  const [remaining, setRemaining] = useState(seconds);

  useEffect(() => {
    if (remaining <= 0) {
      router.replace(href);
      return;
    }
    const timer = setTimeout(() => setRemaining((value) => value - 1), 1000);
    return () => clearTimeout(timer);
  }, [remaining, href, router]);

  return (
    <p aria-live="polite" className="text-sm text-ink-muted">
      Te llevaremos a iniciar sesión en {remaining} s.
    </p>
  );
}
