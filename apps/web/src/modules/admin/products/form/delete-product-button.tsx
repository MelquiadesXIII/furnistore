"use client";

import { useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { deleteProduct } from "@/modules/admin/products/actions";

export function DeleteProductButton({ productId, name }: { productId: number; name: string }) {
  const [confirming, setConfirming] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  if (!confirming) {
    return (
      <Button type="button" variant="outline" onClick={() => setConfirming(true)}>
        Borrar producto
      </Button>
    );
  }

  return (
    <div className="flex flex-col gap-2 rounded-sm border border-brick/40 p-3">
      <p className="text-sm text-ink">¿Borrar «{name}» para siempre? Nunca se vendió, así que no afecta pedidos.</p>
      <div className="flex gap-2">
        <Button
          type="button"
          variant="destructive"
          disabled={pending}
          onClick={() =>
            startTransition(async () => {
              const result = await deleteProduct(productId);
              setError(result.message);
            })
          }
        >
          {pending ? "Borrando…" : "Sí, borrar"}
        </Button>
        <Button type="button" variant="ghost" disabled={pending} onClick={() => setConfirming(false)}>
          No
        </Button>
      </div>
      {error && (
        <p role="alert" className="text-xs text-brick">
          {error}
        </p>
      )}
    </div>
  );
}
