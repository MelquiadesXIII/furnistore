import { defineTable } from "@/modules/admin/reports/tables/table-spec";
import type { OperationsReport } from "@/modules/admin/reports/types";
import { ORDER_STATUS_LABELS } from "@/modules/orders/order-status";

export function operationsTable(report: OperationsReport) {
  return defineTable({
    id: "atrasados",
    title: "Pedidos atrasados",
    empty: "No hay pedidos atrasados.",
    rows: report.overdue,
    href: (row) => `/admin/orders/${row.orderId}`,
    columns: [
      { header: "Pedido", kind: "text", value: (row) => `#${row.orderNumber}` },
      { header: "Cliente", kind: "text", value: (row) => row.customer },
      { header: "Estado", kind: "text", value: (row) => ORDER_STATUS_LABELS[row.status] },
      { header: "Entrega estimada", kind: "date", value: (row) => row.estimatedDeliveryDate },
      { header: "Días de atraso", kind: "count", value: (row) => row.daysLate },
      { header: "Total", kind: "money", value: (row) => row.total },
    ],
  });
}
