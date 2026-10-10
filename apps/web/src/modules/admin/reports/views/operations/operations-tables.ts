import type { ApiSchemas } from "@/lib/api/contract";
import { defineTable, type ReportTableData } from "@/modules/admin/reports/tables/table-spec";
import type { OperationsReport } from "@/modules/admin/reports/types";
import { ORDER_STATUS_LABELS } from "@/modules/orders/order-status";

export const STAGE_LABELS: Record<ApiSchemas["FulfillmentStage"], { title: string; detail: string }> = {
  Queue: { title: "Espera para preparar", detail: "Del pago a la preparación" },
  Preparation: { title: "Preparación", detail: "De la preparación al envío" },
  Transit: { title: "Transporte", detail: "Del envío a la entrega" },
  Total: { title: "Total", detail: "Del pago a la entrega" },
};

export function operationsTables(report: OperationsReport): ReportTableData[] {
  return [
    defineTable({
      id: "etapas",
      title: "Tiempo de cada etapa",
      description: "Solo cuenta los pedidos del período que ya pasaron por esa etapa.",
      empty: "Sin datos.",
      rows: report.stages,
      columns: [
        { header: "Etapa", kind: "text", value: (row) => STAGE_LABELS[row.stage].title },
        { header: "Tramo", kind: "text", value: (row) => STAGE_LABELS[row.stage].detail },
        { header: "Pedidos", kind: "count", value: (row) => row.orders },
        { header: "Promedio", kind: "hours", value: (row) => row.averageHours },
        { header: "Mediana", kind: "hours", value: (row) => row.medianHours },
      ],
    }),
    defineTable({
      id: "atrasados",
      title: "Pedidos atrasados ahora",
      description:
        "Pedidos sin entregar cuya fecha estimada ya pasó, sin importar el período elegido. Del más atrasado al menos.",
      empty: "No hay pedidos atrasados. Todo va en fecha.",
      rows: report.overdue,
      href: (row) => `/admin/orders/${row.orderId}`,
      columns: [
        { header: "Pedido", kind: "text", value: (row) => `#${row.orderNumber}` },
        { header: "Cliente", kind: "text", value: (row) => row.customer },
        { header: "Estado", kind: "text", value: (row) => ORDER_STATUS_LABELS[row.status] },
        { header: "Realizado", kind: "moment", value: (row) => row.placedAt },
        { header: "Entrega estimada", kind: "date", value: (row) => row.estimatedDeliveryDate },
        { header: "Días de atraso", kind: "count", value: (row) => row.daysLate },
        { header: "Total", kind: "money", value: (row) => row.total },
      ],
    }),
  ];
}
