import Link from "next/link";
import { Card, CardContent } from "@/components/ui/card";
import { formatMoment } from "@/lib/format-date";
import { formatPrice } from "@/lib/format-price";
import { ShippingDetails } from "@/modules/account/shipping-details";
import { OrderTimeline } from "@/modules/orders/detail/order-timeline";
import { OrderLineThumb } from "@/modules/orders/order-line-thumb";
import { OrderStatusBadge } from "@/modules/orders/order-status-badge";
import { AuditEntryItem } from "@/modules/admin/audit/audit-entry-item";
import { OrderActionPanel } from "@/modules/admin/orders/detail/order-action-panel";
import type { AdminOrder } from "@/modules/admin/orders/types";

function Section({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <section className="flex flex-col gap-3">
      <h2 className="font-display text-base font-semibold text-ink">{title}</h2>
      <Card className="rounded-sm">
        <CardContent>{children}</CardContent>
      </Card>
    </section>
  );
}

export function AdminOrderView({ detail }: { detail: AdminOrder }) {
  const { order, customer } = detail;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2 border-b border-hairline pb-5">
        <Link href="/admin/orders" className="text-sm text-ink-muted transition-colors hover:text-accent">
          ← Pedidos
        </Link>
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="font-display text-2xl font-semibold tracking-tight text-ink">
            Pedido #{order.orderNumber}
          </h1>
          <OrderStatusBadge status={order.status} />
        </div>
        <p className="text-sm text-ink-muted">
          Realizado el {formatMoment(order.placedAt)} por{" "}
          <Link href={`/admin/customers/${customer.id}`} className="text-ink hover:text-accent">
            {customer.name}
          </Link>{" "}
          ({customer.email})
        </p>
      </div>

      <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="flex flex-col gap-6">
          <section className="flex flex-col gap-3">
            <h2 className="font-display text-base font-semibold text-ink">Productos</h2>
            <Card className="rounded-sm py-0">
              <CardContent className="p-0">
                <ul className="divide-y divide-hairline">
                  {order.lines.map((line) => (
                    <li key={line.productId} className="flex items-center gap-4 p-4">
                      <OrderLineThumb line={line} pixels={48} />
                      <div className="flex min-w-0 flex-1 flex-col">
                        <Link
                          href={`/admin/products/${line.productId}`}
                          className="truncate text-ink hover:text-accent"
                        >
                          {line.productName}
                        </Link>
                        <span className="font-mono text-xs text-ink-muted">
                          {line.quantity} × {formatPrice(line.unitPrice)}
                        </span>
                      </div>
                      <span className="font-mono text-ink">{formatPrice(line.lineTotal)}</span>
                    </li>
                  ))}
                </ul>
                <dl className="flex flex-col gap-1.5 border-t border-hairline p-4 text-sm">
                  <div className="flex justify-between">
                    <dt className="text-ink-muted">Subtotal</dt>
                    <dd className="font-mono">{formatPrice(order.subtotal)}</dd>
                  </div>
                  <div className="flex justify-between">
                    <dt className="text-ink-muted">Envío</dt>
                    <dd>{order.shippingCost === 0 ? "Gratis" : formatPrice(order.shippingCost)}</dd>
                  </div>
                  <div className="flex justify-between border-t border-hairline pt-2">
                    <dt className="font-medium">Total</dt>
                    <dd className="font-mono text-lg">{formatPrice(order.total)}</dd>
                  </div>
                </dl>
              </CardContent>
            </Card>
          </section>

          <Section title="Historial de cambios">
            {detail.history.length === 0 ? (
              <p className="text-sm text-ink-muted">Sin cambios desde que se hizo el pedido.</p>
            ) : (
              <ul className="divide-y divide-hairline">
                {detail.history.map((entry) => (
                  <AuditEntryItem key={entry.id} entry={entry} />
                ))}
              </ul>
            )}
          </Section>
        </div>

        <div className="flex flex-col gap-6">
          <Section title="Acciones">
            <OrderActionPanel orderId={order.id} actions={detail.availableActions} />
          </Section>
          <Section title="Seguimiento">
            <OrderTimeline order={order} />
          </Section>
          <Section title="Enviar a">
            <ShippingDetails name={order.shipTo.name} phone={order.shipTo.phone} address={order.shipTo.address} />
          </Section>
        </div>
      </div>
    </div>
  );
}
