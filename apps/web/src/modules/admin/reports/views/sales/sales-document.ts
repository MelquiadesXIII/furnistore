import type { ReportBlock } from "@/modules/admin/reports/document";
import { formatBucket, formatRange } from "@/modules/admin/reports/format";
import type { SalesReport } from "@/modules/admin/reports/types";
import { salesTables } from "@/modules/admin/reports/views/sales/sales-tables";

export function salesDocument(report: SalesReport): ReportBlock[] {
  const { meta, summary } = report;
  const previousLabel = `Período anterior (${formatRange(meta.previousPeriod.from, meta.previousPeriod.to)})`;
  const label = (point: SalesReport["series"][number]) => formatBucket(point.start, point.end, meta.groupBy);

  return [
    {
      type: "cards",
      columns: 4,
      cards: [
        { type: "metric", label: "Ingresos", kind: "money", metric: summary.revenue },
        { type: "metric", label: "Pedidos", kind: "count", metric: summary.orders },
        { type: "metric", label: "Ticket promedio", kind: "money", metric: summary.averageOrderValue },
        { type: "metric", label: "Unidades vendidas", kind: "count", metric: summary.units },
      ],
    },
    {
      type: "chart",
      title: "Ingresos en el tiempo",
      description:
        "Ventas netas: no incluye pedidos cancelados. La línea punteada es el período anterior de igual duración.",
      chart: {
        kind: "trend",
        variant: "area",
        valueKind: "money",
        valueLabel: "Ingresos",
        previousLabel,
        points: report.series.map((point) => ({
          label: label(point),
          value: point.revenue,
          previous: point.previousRevenue,
        })),
      },
    },
    {
      type: "chart",
      title: "Pedidos en el tiempo",
      chart: {
        kind: "trend",
        variant: "bar",
        valueKind: "count",
        valueLabel: "Pedidos",
        previousLabel,
        points: report.series.map((point) => ({
          label: label(point),
          value: point.orders,
          previous: point.previousOrders,
        })),
      },
    },
    ...salesTables(report).map((table) => ({ type: "table" as const, table })),
  ];
}
