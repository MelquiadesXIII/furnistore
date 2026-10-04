import type { OrderStatus } from "@/modules/orders/types";

export const ORDER_STATUS_LABELS: Record<OrderStatus, string> = {
  Paid: "Pagado",
  Processing: "En preparación",
  Shipped: "Enviado",
  Delivered: "Entregado",
  Cancelled: "Cancelado",
};
