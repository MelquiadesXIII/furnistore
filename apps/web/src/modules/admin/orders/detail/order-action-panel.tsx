"use client";

import { useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import type { AdminActionResult } from "@/modules/admin/action-result";
import { advanceOrder, cancelOrderAsAdmin } from "@/modules/admin/orders/actions";
import { forwardAction } from "@/modules/admin/orders/order-actions";
import type { AdminOrderAction } from "@/modules/admin/orders/types";

export function OrderActionPanel({
  orderId,
  actions,
}: {
  orderId: number;
  actions: AdminOrderAction[];
}) {
  const [cancelling, setCancelling] = useState(false);
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  const next = forwardAction(actions);
  const canCancel = actions.includes("Cancel");

  if (!next && !canCancel) {
    return <p className="text-sm text-ink-muted">Este pedido ya no admite cambios de estado.</p>;
  }

  function run(action: () => Promise<AdminActionResult>) {
    setError(null);
    startTransition(async () => {
      const result = await action();
      if (result.ok) {
        setCancelling(false);
        setReason("");
      } else {
        setError(result.message);
      }
    });
  }

  return (
    <div className="flex flex-col gap-3">
      {next && !cancelling && (
        <Button type="button" disabled={pending} onClick={() => run(() => advanceOrder(orderId, next.path))}>
          {pending ? "Guardando…" : next.label}
        </Button>
      )}

      {canCancel && !cancelling && (
        <Button type="button" variant="outline" disabled={pending} onClick={() => setCancelling(true)}>
          Cancelar pedido
        </Button>
      )}

      {cancelling && (
        <div className="flex flex-col gap-3 rounded-sm border border-brick/40 p-3">
          <p className="text-sm text-ink">
            Se cancela el pedido y el stock vuelve al inventario. No se puede deshacer.
          </p>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="admin-cancel-reason" className="text-ink-muted">
              Motivo para el cliente
            </Label>
            <Textarea
              id="admin-cancel-reason"
              value={reason}
              maxLength={300}
              rows={2}
              disabled={pending}
              onChange={(event) => setReason(event.target.value)}
              className="bg-surface-raised"
            />
          </div>
          <div className="flex flex-col gap-2 sm:flex-row-reverse">
            <Button
              type="button"
              variant="destructive"
              className="flex-1"
              disabled={pending}
              onClick={() => run(() => cancelOrderAsAdmin(orderId, reason))}
            >
              {pending ? "Cancelando…" : "Confirmar cancelación"}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="flex-1"
              disabled={pending}
              onClick={() => {
                setCancelling(false);
                setError(null);
              }}
            >
              Volver
            </Button>
          </div>
        </div>
      )}

      <p role="alert" className="text-sm text-brick empty:hidden">
        {error}
      </p>
    </div>
  );
}
