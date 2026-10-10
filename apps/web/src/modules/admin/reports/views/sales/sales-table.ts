import { formatBucket } from "@/modules/admin/reports/format";
import { defineTable } from "@/modules/admin/reports/tables/table-spec";
import type { SalesReport } from "@/modules/admin/reports/types";

export function salesTable(report: SalesReport) {
  return defineTable({
    id: "ventas",
    title: "Ventas por período",
    empty: "No hubo ventas en este período.",
    rows: report.series,
    columns: [
      { header: "Período", kind: "text", value: (row) => formatBucket(row.start, row.end, report.meta.groupBy) },
      { header: "Pedidos", kind: "count", value: (row) => row.orders },
      { header: "Unidades", kind: "count", value: (row) => row.units },
      { header: "Ingresos", kind: "money", value: (row) => row.revenue },
    ],
  });
}
