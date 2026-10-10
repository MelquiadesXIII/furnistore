import { ReportBarsChart } from "@/modules/admin/reports/charts/bars-chart";
import { formatValue } from "@/modules/admin/reports/format";
import { ReportSection } from "@/modules/admin/reports/shared/report-section";
import { StatCard } from "@/modules/admin/reports/shared/stat-card";
import { ReportTable } from "@/modules/admin/reports/tables/report-table";
import type { CustomersReport } from "@/modules/admin/reports/types";
import { customersTable } from "@/modules/admin/reports/views/customers/customers-table";

export function CustomersReportView({ report }: { report: CustomersReport }) {
  const provinces = report.provinces.slice(0, 10).map((row) => ({ label: row.name, value: row.revenue }));

  return (
    <div className="flex flex-col gap-4">
      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard label="Compradores" value={formatValue("count", report.buyers)} />
        <StatCard label="Nuevos" value={formatValue("count", report.newBuyers)} hint="Su primera compra" />
        <StatCard label="Recurrentes" value={formatValue("count", report.returningBuyers)} hint="Ya habían comprado" />
      </div>

      <ReportSection title="Ingresos por provincia">
        {provinces.length === 0 ? (
          <p className="text-sm text-ink-muted">No hubo ventas en este período.</p>
        ) : (
          <ReportBarsChart items={provinces} kind="money" label="Ingresos" horizontal />
        )}
      </ReportSection>

      <ReportTable table={customersTable(report)} />
    </div>
  );
}
