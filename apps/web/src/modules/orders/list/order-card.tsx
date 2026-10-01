import { ChevronRight } from "lucide-react";
import Link from "next/link";
import { Card, CardContent } from "@/components/ui/card";
import { formatCalendarDate, formatMomentDay } from "@/lib/format-date";
import { formatPrice } from "@/lib/format-price";
import { OrderLineThumb } from "@/modules/orders/order-line-thumb";
import { OrderStatusBadge } from "@/modules/orders/order-status-badge";
import type { Order } from "@/modules/orders/types";

const MAX_THUMBS = 4;

export function OrderCard({ order }: { order: Order }) {
  const units = order.lines.reduce((total, line) => total + line.quantity, 0);
  const hidden = order.lines.length - MAX_THUMBS;
  const inTransit = order.status === "Paid" || order.status === "Shipped";

  return (
    <li>
      <Link
        href={`/orders/${order.id}`}
        aria-label={`Ver el pedido #${order.orderNumber}`}
        className="group block rounded-sm focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
      >
        <Card className="rounded-sm py-0 transition-colors group-hover:ring-accent">
          <CardContent className="flex flex-col gap-4 p-4 sm:flex-row sm:items-center">
            <div className="flex min-w-0 flex-1 flex-col gap-2">
              <div className="flex flex-wrap items-center gap-3">
                <span className="font-display font-medium text-ink">
                  Pedido #{order.orderNumber}
                </span>
                <OrderStatusBadge status={order.status} />
              </div>
              <span className="text-sm text-ink-muted">
                Realizado el {formatMomentDay(order.placedAt)}
                {inTransit &&
                  ` · Entrega estimada: ${formatCalendarDate(order.estimatedDeliveryDate)}`}
              </span>
              <div className="flex items-center gap-2">
                {order.lines.slice(0, MAX_THUMBS).map((line) => (
                  <OrderLineThumb key={line.productId} line={line} pixels={48} />
                ))}
                {hidden > 0 && (
                  <span className="font-mono text-xs text-ink-muted">+{hidden}</span>
                )}
              </div>
            </div>

            <div className="flex items-center justify-between gap-4 sm:flex-col sm:items-end">
              <span className="text-sm text-ink-muted">
                {units === 1 ? "1 artículo" : `${units} artículos`}
              </span>
              <span className="font-mono text-lg text-ink">{formatPrice(order.total)}</span>
            </div>

            <ChevronRight
              aria-hidden="true"
              className="hidden size-5 text-ink-muted transition-colors group-hover:text-accent sm:block"
            />
          </CardContent>
        </Card>
      </Link>
    </li>
  );
}
