import { ReportBarsChart } from "@/modules/admin/reports/charts/bars-chart";
import { formatValue } from "@/modules/admin/reports/format";
import { ReportSection } from "@/modules/admin/reports/shared/report-section";
import { StatCard } from "@/modules/admin/reports/shared/stat-card";
import { ReportTable } from "@/modules/admin/reports/tables/report-table";
import type { OperationsReport } from "@/modules/admin/reports/types";
import { operationsTable } from "@/modules/admin/reports/views/operations/operations-table";
import { ORDER_STATUS_LABELS } from "@/modules/orders/order-status";

const STATUS_COLORS = {
  Paid: "var(--chart-1)",
  Processing: "var(--chart-4)",
  Shipped: "var(--chart-3)",
  Delivered: "var(--chart-2)",
  Cancelled: "var(--chart-6)",
};

export function OperationsReportView({ report }: { report: OperationsReport }) {
  const bars = report.statuses.map((row) => ({
    label: ORDER_STATUS_LABELS[row.status],
    value: row.orders,
    color: STATUS_COLORS[row.status],
  }));

  return (
    <div className="flex flex-col gap-4">
      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard label="Pedidos del período" value={formatValue("count", report.orders)} />
        <StatCard
          label="Entregados a tiempo"
          value={formatValue("percent", report.onTimeRate)}
          hint={`${report.deliveredOnTime} de ${report.delivered} entregados`}
        />
        <StatCard label="Atrasados ahora" value={formatValue("count", report.overdue.length)} />
      </div>

      <ReportSection title="Pedidos por estado">
        <ReportBarsChart items={bars} kind="count" label="Pedidos" />
      </ReportSection>

      <ReportTable table={operationsTable(report)} />
    </div>
  );
}
