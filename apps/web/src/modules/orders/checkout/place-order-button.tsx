"use client";

import { useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { placeOrder } from "@/modules/orders/actions";

export function PlaceOrderButton({
  expectedTotal,
  disabled,
}: {
  expectedTotal: number;
  disabled: boolean;
}) {
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function handleClick() {
    setError(null);
    startTransition(async () => {
      const result = await placeOrder(expectedTotal);
      if (!result.ok) setError(result.message);
    });
  }

  return (
    <div className="flex flex-col gap-2">
      <Button type="button" className="w-full" disabled={disabled || pending} onClick={handleClick}>
        {pending ? "Confirmando…" : "Confirmar pedido"}
      </Button>
      {disabled && !error && (
        <p className="text-center text-xs text-ink-muted">
          Guarda tus datos de envío para confirmar.
        </p>
      )}
      <p role="alert" className="text-sm text-brick empty:hidden">
        {error}
      </p>
    </div>
  );
}
