import type { OrderStatus } from "@/modules/orders/types";

export const ORDER_STATUS_LABELS: Record<OrderStatus, string> = {
  Paid: "Pagado",
  Shipped: "Enviado",
  Delivered: "Entregado",
  Cancelled: "Cancelado",
};
