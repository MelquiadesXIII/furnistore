import type { ApiSchemas } from "@/lib/api/contract";
import { defineTable, type ReportTableData } from "@/modules/admin/reports/tables/table-spec";
import type { CancellationsReport } from "@/modules/admin/reports/types";
import { ORDER_STATUS_LABELS } from "@/modules/orders/order-status";
import type { OrderStatus } from "@/modules/orders/types";

export const ACTOR_LABELS: Record<ApiSchemas["CancellationActor"], string> = {
  Customer: "Cliente",
  Admin: "Administración",
  Unknown: "Sin registro",
};

export function stageLabel(stage: OrderStatus | null): string {
  return stage === null ? "Sin registro" : `Estando ${ORDER_STATUS_LABELS[stage].toLowerCase()}`;
}

export function reasonLabel(reason: string | null): string {
  return reason ?? "Sin motivo";
}

export function cancellationsTables(report: CancellationsReport): ReportTableData[] {
  return [
    defineTable({
      id: "motivos",
      title: "Motivos más frecuentes",
      description: "Agrupa motivos escritos igual, sin distinguir mayúsculas.",
      empty: "No hubo cancelaciones en este período.",
      rows: report.reasons,
      columns: [
        { header: "Motivo", kind: "text", value: (row) => reasonLabel(row.reason) },
        { header: "Pedidos", kind: "count", value: (row) => row.orders },
        { header: "% de cancelaciones", kind: "percent", value: (row) => row.share },
      ],
    }),
    defineTable({
      id: "pedidos",
      title: "Pedidos cancelados",
      description: "Pedidos realizados en el período que terminaron cancelados, del más reciente al más antiguo.",
      empty: "No hubo cancelaciones en este período.",
      rows: report.orders,
      href: (row) => `/admin/orders/${row.orderId}`,
      columns: [
        { header: "Pedido", kind: "text", value: (row) => `#${row.orderNumber}` },
        { header: "Cliente", kind: "text", value: (row) => row.customer },
        { header: "Realizado", kind: "moment", value: (row) => row.placedAt },
        { header: "Cancelado", kind: "moment", value: (row) => row.cancelledAt },
        { header: "Por", kind: "text", value: (row) => ACTOR_LABELS[row.cancelledBy] },
        { header: "Etapa", kind: "text", value: (row) => (row.stage ? ORDER_STATUS_LABELS[row.stage] : null) },
        { header: "Motivo", kind: "text", value: (row) => row.reason },
        { header: "Total", kind: "money", value: (row) => row.total },
      ],
    }),
  ];
}
