import { CircleCheck } from "lucide-react";
import Link from "next/link";
import { Card, CardContent } from "@/components/ui/card";
import { formatCalendarDate, formatMoment } from "@/lib/format-date";
import { formatPrice } from "@/lib/format-price";
import { ShippingDetails } from "@/modules/account/shipping-details";
import { buildProductHref } from "@/modules/products/slug";
import { CancelOrderButton } from "@/modules/orders/detail/cancel-order-button";
import { OrderTimeline } from "@/modules/orders/detail/order-timeline";
import { OrderLineThumb } from "@/modules/orders/order-line-thumb";
import { OrderStatusBadge } from "@/modules/orders/order-status-badge";
import type { Order } from "@/modules/orders/types";

function SectionTitle({ children }: { children: string }) {
  return <h2 className="font-display text-lg font-semibold text-ink">{children}</h2>;
}

export function OrderDetailView({ order, justPlaced }: { order: Order; justPlaced: boolean }) {
  return (
    <div className="flex flex-col gap-8">
      <div className="flex flex-col gap-2 border-b border-hairline pb-6">
        <Link href="/orders" className="text-sm text-ink-muted transition-colors hover:text-accent">
          ← Mis pedidos
        </Link>
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="font-display text-3xl font-semibold tracking-tight text-ink">
            Pedido #{order.orderNumber}
          </h1>
          <OrderStatusBadge status={order.status} />
        </div>
        <p className="text-sm text-ink-muted">Realizado el {formatMoment(order.placedAt)}</p>
      </div>

      {justPlaced && order.status === "Paid" && (
        <div
          role="status"
          className="flex items-start gap-3 rounded-sm border border-accent/40 bg-accent/10 p-4"
        >
          <CircleCheck aria-hidden="true" className="mt-0.5 size-5 shrink-0 text-accent" />
          <div className="flex flex-col gap-1">
            <p className="font-medium text-ink">¡Gracias por tu compra!</p>
            <p className="text-sm text-ink-muted">
              Tu pedido está confirmado. Te lo entregaremos alrededor del{" "}
              {formatCalendarDate(order.estimatedDeliveryDate)}.
            </p>
          </div>
        </div>
      )}

      <div className="grid items-start gap-8 lg:grid-cols-[minmax(0,1fr)_20rem]">
        <section className="flex flex-col gap-4">
          <SectionTitle>Productos</SectionTitle>
          <Card className="rounded-sm py-0">
            <CardContent className="flex flex-col p-0">
              <ul className="flex flex-col divide-y divide-hairline">
                {order.lines.map((line) => (
                  <li key={line.productId} className="flex items-center gap-4 p-4">
                    <OrderLineThumb line={line} pixels={64} />
                    <div className="flex min-w-0 flex-1 flex-col gap-1">
                      <Link
                        href={buildProductHref({ id: line.productId, name: line.productName })}
                        className="truncate font-display font-medium text-ink transition-colors hover:text-accent"
                      >
                        {line.productName}
                      </Link>
                      <span className="font-mono text-sm text-ink-muted">
                        {line.quantity} × {formatPrice(line.unitPrice)}
                      </span>
                    </div>
                    <span className="font-mono text-ink">{formatPrice(line.lineTotal)}</span>
                  </li>
                ))}
              </ul>

              <dl className="flex flex-col gap-2 border-t border-hairline p-4 text-sm">
                <div className="flex justify-between gap-4">
                  <dt className="text-ink-muted">Subtotal</dt>
                  <dd className="font-mono text-ink">{formatPrice(order.subtotal)}</dd>
                </div>
                <div className="flex justify-between gap-4">
                  <dt className="text-ink-muted">Envío</dt>
                  <dd className="text-ink">
                    {order.shippingCost === 0 ? "Gratis" : formatPrice(order.shippingCost)}
                  </dd>
                </div>
                <div className="flex justify-between gap-4 border-t border-hairline pt-3">
                  <dt className="font-medium text-ink">Total</dt>
                  <dd className="font-mono text-lg text-ink">{formatPrice(order.total)}</dd>
                </div>
              </dl>
            </CardContent>
          </Card>
        </section>

        <div className="flex flex-col gap-8">
          <section className="flex flex-col gap-4">
            <SectionTitle>Seguimiento</SectionTitle>
            <Card className="rounded-sm">
              <CardContent>
                <OrderTimeline order={order} />
              </CardContent>
            </Card>
          </section>

          <section className="flex flex-col gap-4">
            <SectionTitle>Envío</SectionTitle>
            <Card className="rounded-sm">
              <CardContent>
                <ShippingDetails
                  name={order.shipTo.name}
                  phone={order.shipTo.phone}
                  address={order.shipTo.address}
                />
              </CardContent>
            </Card>
          </section>

          {order.canCancel && (
            <section className="flex flex-col gap-2">
              <p className="text-xs text-ink-muted">
                Puedes cancelar el pedido mientras no hayamos empezado a prepararlo.
              </p>
              <CancelOrderButton orderId={order.id} />
            </section>
          )}
        </div>
      </div>
    </div>
  );
}
