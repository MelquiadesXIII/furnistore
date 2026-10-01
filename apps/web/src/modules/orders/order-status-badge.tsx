import { cn } from "cn";
import { Badge } from "@/components/ui/badge";
import { ORDER_STATUS_LABELS } from "@/modules/orders/order-status";
import type { OrderStatus } from "@/modules/orders/types";

const STATUS_STYLES: Record<OrderStatus, string> = {
  Paid: "border-accent text-accent",
  Shipped: "bg-accent text-accent-ink",
  Delivered: "bg-accent/15 text-accent",
  Cancelled: "bg-brick/10 text-brick",
};

export function OrderStatusBadge({ status }: { status: OrderStatus }) {
  return (
    <Badge variant="outline" className={cn("rounded-sm", STATUS_STYLES[status])}>
      {ORDER_STATUS_LABELS[status]}
    </Badge>
  );
}
