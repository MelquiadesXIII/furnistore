"use client";

import { useEffect } from "react";
import { Button } from "@/components/ui/button";
import { ChairMark } from "@/components/furniture-marks";

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error("[shop] error:", error);
  }, [error]);

  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-4 px-6 py-32 text-center">
      <ChairMark className="h-16 w-16 text-brick" />
      <h1 className="font-display text-2xl font-semibold text-ink">
        Algo se rompió
      </h1>
      <p className="max-w-md text-sm text-ink-muted">
        No pudimos cargar esta sección. Intenta de nuevo en un momento.
      </p>
      <Button onClick={reset}>Reintentar</Button>
    </div>
  );
}