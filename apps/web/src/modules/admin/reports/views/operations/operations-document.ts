import type { ReportBlock, Tone } from "@/modules/admin/reports/document";
import { formatValue } from "@/modules/admin/reports/format";
import type { OperationsReport } from "@/modules/admin/reports/types";
import { operationsTables, STAGE_LABELS } from "@/modules/admin/reports/views/operations/operations-tables";
import { ORDER_STATUS_LABELS } from "@/modules/orders/order-status";
import type { OrderStatus } from "@/modules/orders/types";

const STATUS_TONES: Record<OrderStatus, Tone> = {
  Paid: "chart-1",
  Processing: "chart-4",
  Shipped: "chart-3",
  Delivered: "chart-2",
  Cancelled: "chart-6",
};

const STAGE_TONES: Tone[] = ["chart-1", "chart-2", "chart-3"];

export function operationsDocument(report: OperationsReport): ReportBlock[] {
  const total = report.stages.find((stage) => stage.stage === "Total");
  const stages = report.stages.filter((stage) => stage.stage !== "Total" && stage.averageHours !== null);

  return [
    {
      type: "cards",
      columns: 4,
      cards: [
        { type: "stat", label: "Pedidos del período", value: formatValue("count", report.orders) },
        {
          type: "stat",
          label: "Entregados a tiempo",
          value: formatValue("percent", report.onTime.onTimeRate),
          hint: `${formatValue("count", report.onTime.onTime)} de ${formatValue("count", report.onTime.delivered)} entregados`,
        },
        {
          type: "stat",
          label: "Del pago a la entrega",
          value: formatValue("hours", total?.averageHours),
          hint: total?.medianHours != null ? `Mediana ${formatValue("hours", total.medianHours)}` : "Sin entregas",
        },
        {
          type: "stat",
          label: "Atrasados ahora",
          value: formatValue("count", report.overdue.length),
          hint:
            report.onTime.averageDaysLate != null
              ? `Las entregas tardías llegaron ${formatValue("days", report.onTime.averageDaysLate)} tarde en promedio`
              : "Sin entregas tardías en el período",
        },
      ],
    },
    {
      type: "pair",
      blocks: [
        {
          type: "chart",
          title: "Pedidos por estado",
          description: "Estado actual de los pedidos realizados en el período.",
          chart: {
            kind: "bars",
            layout: "vertical",
            valueKind: "count",
            valueLabel: "Pedidos",
            items: report.statuses.map((row) => ({
              label: ORDER_STATUS_LABELS[row.status],
              value: row.orders,
              tone: STATUS_TONES[row.status],
            })),
          },
        },
        {
          type: "chart",
          title: "Tiempo promedio por etapa",
          description: "Dónde se va el tiempo entre el pago y la entrega.",
          empty: "Ningún pedido del período avanzó todavía.",
          chart:
            stages.length === 0
              ? null
              : {
                  kind: "bars",
                  layout: "horizontal",
                  valueKind: "hours",
                  valueLabel: "Promedio",
                  items: stages.map((row, index) => ({
                    label: STAGE_LABELS[row.stage].title,
                    value: row.averageHours ?? 0,
                    tone: STAGE_TONES[index % STAGE_TONES.length],
                  })),
                },
        },
      ],
    },
    ...operationsTables(report).map((table) => ({ type: "table" as const, table })),
  ];
}
