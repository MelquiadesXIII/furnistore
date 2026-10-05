"use client";

import { useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { advanceOrder } from "@/modules/admin/orders/actions";

export function OrderQuickAction({
  orderId,
  label,
  path,
}: {
  orderId: number;
  label: string;
  path: "prepare" | "ship" | "deliver";
}) {
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  return (
    <div className="flex flex-col items-end gap-1">
      <Button
        type="button"
        size="sm"
        variant="outline"
        disabled={pending}
        onClick={() => {
          setError(null);
          startTransition(async () => {
            const result = await advanceOrder(orderId, path);
            if (!result.ok) setError(result.message);
          });
        }}
      >
        {pending ? "Guardando…" : label}
      </Button>
      {error && (
        <span role="alert" className="max-w-48 text-right text-xs text-brick">
          {error}
        </span>
      )}
    </div>
  );
}
