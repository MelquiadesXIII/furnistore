import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { formatPrice } from "@/lib/format-price";

const UPDATING = "Actualizando…";

export function CartSummary({
  subtotal,
  shippingCost,
  total,
  itemCount,
  pending,
  blocked,
}: {
  subtotal: number;
  shippingCost: number;
  total: number;
  itemCount: number;
  pending: boolean;
  blocked: boolean;
}) {
  const shipping = shippingCost === 0 ? "Gratis" : formatPrice(shippingCost);

  return (
    <Card className="rounded-sm lg:sticky lg:top-24">
      <CardContent className="flex flex-col gap-4">
        <h2 className="font-display text-lg font-semibold text-ink">Resumen</h2>

        <dl className="flex flex-col gap-2 text-sm">
          <div className="flex justify-between gap-4">
            <dt className="text-ink-muted">
              {itemCount === 1 ? "1 artículo" : `${itemCount} artículos`}
            </dt>
            <dd className="font-mono text-ink">{pending ? UPDATING : formatPrice(subtotal)}</dd>
          </div>
          <div className="flex justify-between gap-4">
            <dt className="text-ink-muted">Envío</dt>
            <dd className="text-ink">{pending ? UPDATING : shipping}</dd>
          </div>
        </dl>

        <div
          aria-live="polite"
          className="flex justify-between gap-4 border-t border-hairline pt-4"
        >
          <span className="font-medium text-ink">Total</span>
          <span className="font-mono text-lg text-ink">{pending ? UPDATING : formatPrice(total)}</span>
        </div>

        {pending || blocked ? (
          <Button disabled className="w-full">
            Continuar al pago
          </Button>
        ) : (
          <Button asChild className="w-full">
            <Link href="/checkout">Continuar al pago</Link>
          </Button>
        )}
        {blocked && (
          <p className="text-center text-xs text-ink-muted">
            Resuelve los avisos de tu carrito para continuar.
          </p>
        )}
      </CardContent>
    </Card>
  );
}
