import { ReportAreaChart } from "@/modules/admin/reports/charts/area-chart";
import { formatBucket, formatValue } from "@/modules/admin/reports/format";
import { ReportSection } from "@/modules/admin/reports/shared/report-section";
import { StatCard } from "@/modules/admin/reports/shared/stat-card";
import { ReportTable } from "@/modules/admin/reports/tables/report-table";
import type { SalesReport } from "@/modules/admin/reports/types";
import { salesTable } from "@/modules/admin/reports/views/sales/sales-table";

export function SalesReportView({ report }: { report: SalesReport }) {
  const points = report.series.map((point) => ({
    label: formatBucket(point.start, point.end, report.meta.groupBy),
    value: point.revenue,
  }));

  return (
    <div className="flex flex-col gap-4">
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Ingresos" value={formatValue("money", report.revenue)} hint="Sin pedidos cancelados" />
        <StatCard label="Pedidos" value={formatValue("count", report.orders)} />
        <StatCard label="Ticket promedio" value={formatValue("money", report.averageOrderValue)} />
        <StatCard label="Unidades vendidas" value={formatValue("count", report.units)} />
      </div>

      <ReportSection title="Ingresos en el tiempo">
        <ReportAreaChart points={points} kind="money" label="Ingresos" />
      </ReportSection>

      <ReportTable table={salesTable(report)} />
    </div>
  );
}
