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
  inCart = 0,
  isAuthenticated,
  returnTo,
  withQuantity = false,
}: {
  productId: number;
  productName: string;
  stock: number;
  inCart?: number;
  isAuthenticated: boolean;
  returnTo: string;
  withQuantity?: boolean;
}) {
  const [quantity, setQuantity] = useState("1");
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

  const available = stock - inCart;

  if (available < 1) {
    return (
      <Button asChild variant="outline" className="w-full">
        <Link href="/cart">Ya tienes todo el stock en tu carrito</Link>
      </Button>
    );
  }

  function handleAdd() {
    setError(null);
    const parsedQuantity = Number.parseInt(quantity, 10);
    if (!Number.isInteger(parsedQuantity) || parsedQuantity < 1 || parsedQuantity > available) {
      setError(`Introduce una cantidad entre 1 y ${available}.`);
      return;
    }

    startTransition(async () => {
      const result = await addToCart(productId, parsedQuantity, returnTo);
      if (result.ok) {
        setAdded(true);
        setQuantity("1");
      } else {
        setError(result.message);
      }
    });
  }

  function adjustQuantity(delta: number) {
    const current = Number.parseInt(quantity, 10);
    const base = Number.isInteger(current) ? current : 1;
    setQuantity(String(Math.min(Math.max(base + delta, 1), available)));
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
              disabled={pending || Number(quantity) <= 1}
              onClick={() => adjustQuantity(-1)}
            >
              <Minus />
            </Button>
            <input
              type="number"
              min={1}
              max={available}
              inputMode="numeric"
              aria-label={`Cantidad de ${productName}`}
              value={quantity}
              disabled={pending}
              onChange={(event) => setQuantity(event.target.value)}
              onBlur={() => {
                const parsed = Number.parseInt(quantity, 10);
                setQuantity(String(Number.isInteger(parsed) ? Math.min(Math.max(parsed, 1), available) : 1));
              }}
              className="h-8 w-8 appearance-none border-0 bg-transparent p-0 text-center font-mono tabular-nums text-ink outline-none [&::-webkit-inner-spin-button]:appearance-none [&::-webkit-outer-spin-button]:appearance-none"
            />
            <Button
              type="button"
              variant="outline"
              size="icon"
              aria-label="Agregar una unidad"
              disabled={pending || Number(quantity) >= available}
              onClick={() => adjustQuantity(1)}
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
