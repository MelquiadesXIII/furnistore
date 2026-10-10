import type { ReportBlock } from "@/modules/admin/reports/document";
import { formatBucket } from "@/modules/admin/reports/format";
import type { CustomersReport } from "@/modules/admin/reports/types";
import { customersTables } from "@/modules/admin/reports/views/customers/customers-tables";

const TOP_PROVINCES = 10;

export function customersDocument(report: CustomersReport): ReportBlock[] {
  return [
    {
      type: "cards",
      columns: 4,
      cards: [
        { type: "metric", label: "Compradores", kind: "count", metric: report.buyers },
        { type: "metric", label: "Nuevos", kind: "count", metric: report.newBuyers },
        { type: "metric", label: "Recurrentes", kind: "count", metric: report.returningBuyers },
        { type: "metric", label: "Ingreso por comprador", kind: "money", metric: report.revenuePerBuyer },
      ],
    },
    {
      type: "chart",
      title: "Compradores nuevos y recurrentes",
      description: "Nuevo: su primera compra cae en ese tramo. Recurrente: ya había comprado antes.",
      chart: {
        kind: "stacked",
        valueKind: "count",
        series: [
          { label: "Nuevos", tone: "chart-1" },
          { label: "Recurrentes", tone: "chart-2" },
        ],
        points: report.series.map((point) => ({
          label: formatBucket(point.start, point.end, report.meta.groupBy),
          values: [point.newBuyers, point.returningBuyers],
        })),
      },
    },
    {
      type: "chart",
      title: "Ingresos por provincia",
      empty: "No hubo ventas en este período.",
      chart:
        report.provinces.length === 0
          ? null
          : {
              kind: "bars",
              layout: "horizontal",
              valueKind: "money",
              valueLabel: "Ingresos",
              items: report.provinces
                .slice(0, TOP_PROVINCES)
                .map((row) => ({ label: row.name, value: row.revenue, tone: "chart-1" })),
            },
    },
    ...customersTables(report).map((table) => ({ type: "table" as const, table })),
  ];
}
