import { ReportBarsChart } from "@/modules/admin/reports/charts/bars-chart";
import { formatValue } from "@/modules/admin/reports/format";
import { ReportSection } from "@/modules/admin/reports/shared/report-section";
import { StatCard } from "@/modules/admin/reports/shared/stat-card";
import { ReportTable } from "@/modules/admin/reports/tables/report-table";
import type { ProductsReport } from "@/modules/admin/reports/types";
import { productsTable } from "@/modules/admin/reports/views/products/products-table";

export function ProductsReportView({ report }: { report: ProductsReport }) {
  const top = report.products.slice(0, 10).map((row) => ({ label: row.name, value: row.revenue }));

  return (
    <div className="flex flex-col gap-4">
      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard label="Ingresos por productos" value={formatValue("money", report.revenue)} hint="Sin envío" />
        <StatCard label="Productos vendidos" value={formatValue("count", report.products.length)} />
        <StatCard label="Activos sin ventas" value={formatValue("count", report.unsoldProducts)} />
      </div>

      <ReportSection title="Top 10 por ingresos">
        {top.length === 0 ? (
          <p className="text-sm text-ink-muted">No hubo ventas en este período.</p>
        ) : (
          <ReportBarsChart items={top} kind="money" label="Ingresos" horizontal />
        )}
      </ReportSection>

      <ReportTable table={productsTable(report)} />
    </div>
  );
}
