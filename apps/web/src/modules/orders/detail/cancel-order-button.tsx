"use client";

import { useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { cancelOrder } from "@/modules/orders/actions";

export function CancelOrderButton({ orderId }: { orderId: number }) {
  const [confirming, setConfirming] = useState(false);
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function handleConfirm() {
    setError(null);
    startTransition(async () => {
      const result = await cancelOrder(orderId, reason);
      if (result.ok) {
        setConfirming(false);
        setReason("");
      } else {
        setError(result.message);
      }
    });
  }

  if (!confirming) {
    return (
      <Button
        type="button"
        variant="outline"
        className="w-full"
        onClick={() => setConfirming(true)}
      >
        Cancelar pedido
      </Button>
    );
  }

  return (
    <div className="flex flex-col gap-3">
      <p className="text-sm text-ink">
        ¿Seguro que quieres cancelar este pedido? Esta acción no se puede deshacer.
      </p>
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="cancel-reason" className="text-ink-muted">
          Motivo (opcional)
        </Label>
        <Input
          id="cancel-reason"
          value={reason}
          maxLength={300}
          disabled={pending}
          onChange={(event) => setReason(event.target.value)}
          className="bg-surface-raised"
        />
      </div>
      <p role="alert" className="text-sm text-brick empty:hidden">
        {error}
      </p>
      <div className="flex flex-col gap-2 sm:flex-row-reverse">
        <Button
          type="button"
          variant="destructive"
          className="flex-1"
          disabled={pending}
          onClick={handleConfirm}
        >
          {pending ? "Cancelando…" : "Sí, cancelar"}
        </Button>
        <Button
          type="button"
          variant="ghost"
          className="flex-1"
          disabled={pending}
          onClick={() => {
            setConfirming(false);
            setError(null);
          }}
        >
          No, mantenerlo
        </Button>
      </div>
    </div>
  );
}
