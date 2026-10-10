import { formatBucket, formatRange } from "@/modules/admin/reports/format";
import { defineTable, type ReportTableData } from "@/modules/admin/reports/tables/table-spec";
import type { SalesReport } from "@/modules/admin/reports/types";

export function salesTables(report: SalesReport): ReportTableData[] {
  const grouping = report.meta.groupBy;

  return [
    defineTable({
      id: "desglose",
      title: "Desglose del período",
      description: `Cada fila es un tramo del período; la última columna repite los ingresos del mismo tramo en ${formatRange(report.meta.previousPeriod.from, report.meta.previousPeriod.to)}.`,
      empty: "No hubo ventas en este período.",
      rows: report.series,
      columns: [
        { header: "Período", kind: "text", value: (point) => formatBucket(point.start, point.end, grouping) },
        { header: "Pedidos", kind: "count", value: (point) => point.orders },
        { header: "Unidades", kind: "count", value: (point) => point.units },
        { header: "Ingresos", kind: "money", value: (point) => point.revenue },
        { header: "Ticket promedio", kind: "money", value: (point) => point.averageOrderValue },
        { header: "Ingresos antes", kind: "money", value: (point) => point.previousRevenue },
      ],
    }),
  ];
}
