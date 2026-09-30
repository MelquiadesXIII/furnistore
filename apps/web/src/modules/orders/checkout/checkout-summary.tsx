import { Card, CardContent } from "@/components/ui/card";
import { formatPrice } from "@/lib/format-price";
import type { Cart } from "@/modules/cart/types";
import { PlaceOrderButton } from "@/modules/orders/checkout/place-order-button";

export function CheckoutSummary({ cart, canPlaceOrder }: { cart: Cart; canPlaceOrder: boolean }) {
  return (
    <Card className="rounded-sm lg:sticky lg:top-24">
      <CardContent className="flex flex-col gap-4">
        <h2 className="font-display text-lg font-semibold text-ink">Tu pedido</h2>

        <ul className="flex flex-col gap-3 text-sm">
          {cart.items.map((item) => (
            <li key={item.productId} className="flex justify-between gap-4">
              <span className="min-w-0 text-ink">
                <span className="line-clamp-2">{item.productName}</span>
                <span className="font-mono text-xs text-ink-muted">
                  {item.quantity} × {formatPrice(item.unitPrice)}
                </span>
              </span>
              <span className="shrink-0 font-mono text-ink">{formatPrice(item.lineTotal)}</span>
            </li>
          ))}
        </ul>

        <dl className="flex flex-col gap-2 border-t border-hairline pt-4 text-sm">
          <div className="flex justify-between gap-4">
            <dt className="text-ink-muted">Subtotal</dt>
            <dd className="font-mono text-ink">{formatPrice(cart.subtotal)}</dd>
          </div>
          <div className="flex justify-between gap-4">
            <dt className="text-ink-muted">Envío</dt>
            <dd className="text-ink">
              {cart.shippingCost === 0 ? "Gratis" : formatPrice(cart.shippingCost)}
            </dd>
          </div>
        </dl>

        <div className="flex justify-between gap-4 border-t border-hairline pt-4">
          <span className="font-medium text-ink">Total</span>
          <span className="font-mono text-lg text-ink">{formatPrice(cart.total)}</span>
        </div>

        <p className="rounded-sm border border-dashed border-hairline p-3 text-xs text-ink-muted">
          Pago simulado: al confirmar, el pedido queda pagado sin cobrarte nada.
        </p>

        <PlaceOrderButton expectedTotal={cart.total} disabled={!canPlaceOrder} />
      </CardContent>
    </Card>
  );
}
