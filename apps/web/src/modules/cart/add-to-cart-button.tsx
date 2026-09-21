"use client";

import { Check, Minus, Plus } from "lucide-react";
import Link from "next/link";
import { useEffect, useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { addToCart } from "@/modules/cart/actions";

export function AddToCartButton({
  productId,
  productName,
  stock,
  isAuthenticated,
  returnTo,
  withQuantity = false,
}: {
  productId: number;
  productName: string;
  stock: number;
  isAuthenticated: boolean;
  returnTo: string;
  withQuantity?: boolean;
}) {
  const [quantity, setQuantity] = useState(1);
  const [added, setAdded] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  useEffect(() => {
    if (!added) return;
    const timer = setTimeout(() => setAdded(false), 2000);
    return () => clearTimeout(timer);
  }, [added]);

  if (stock < 1) {
    return (
      <Button disabled variant="outline" className="w-full">
        Agotado
      </Button>
    );
  }

  if (!isAuthenticated) {
    return (
      <Button asChild className="w-full">
        <Link href={`/login?next=${encodeURIComponent(returnTo)}`}>Agregar al carrito</Link>
      </Button>
    );
  }

  function handleAdd() {
    setError(null);
    startTransition(async () => {
      const result = await addToCart(productId, quantity, returnTo);
      if (result.ok) {
        setAdded(true);
        setQuantity(1);
      } else {
        setError(result.message);
      }
    });
  }

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-center gap-2">
        {withQuantity && (
          <div
            role="group"
            aria-label={`Cantidad de ${productName}`}
            className="flex items-center gap-1"
          >
            <Button
              type="button"
              variant="outline"
              size="icon"
              aria-label="Quitar una unidad"
              disabled={pending || quantity <= 1}
              onClick={() => setQuantity((current) => current - 1)}
            >
              <Minus />
            </Button>
            <span className="w-8 text-center font-mono tabular-nums text-ink">{quantity}</span>
            <Button
              type="button"
              variant="outline"
              size="icon"
              aria-label="Agregar una unidad"
              disabled={pending || quantity >= stock}
              onClick={() => setQuantity((current) => current + 1)}
            >
              <Plus />
            </Button>
          </div>
        )}
        <Button type="button" className="flex-1" disabled={pending} onClick={handleAdd}>
          {added ? (
            <>
              <Check />
              Agregado
            </>
          ) : pending ? (
            "Agregando…"
          ) : (
            "Agregar al carrito"
          )}
        </Button>
      </div>
      <p aria-live="polite" className="min-h-0 text-sm text-brick empty:hidden">
        {error}
      </p>
    </div>
  );
}
