import { defineTable } from "@/modules/admin/reports/tables/table-spec";
import type { CancellationsReport } from "@/modules/admin/reports/types";

export const ACTOR_LABELS = {
  Customer: "Cliente",
  Admin: "Administración",
  Unknown: "Sin registro",
};

export function cancellationsTable(report: CancellationsReport) {
  return defineTable({
    id: "cancelados",
    title: "Pedidos cancelados",
    empty: "No hubo cancelaciones en este período.",
    rows: report.orders,
    href: (row) => `/admin/orders/${row.orderId}`,
    columns: [
      { header: "Pedido", kind: "text", value: (row) => `#${row.orderNumber}` },
      { header: "Cliente", kind: "text", value: (row) => row.customer },
      { header: "Cancelado", kind: "moment", value: (row) => row.cancelledAt },
      { header: "Por", kind: "text", value: (row) => ACTOR_LABELS[row.cancelledBy] },
      { header: "Motivo", kind: "text", value: (row) => row.reason },
      { header: "Total", kind: "money", value: (row) => row.total },
    ],
  });
}
