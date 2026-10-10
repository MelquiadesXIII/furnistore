import { ReportDonutChart } from "@/modules/admin/reports/charts/donut-chart";
import { formatValue } from "@/modules/admin/reports/format";
import { ReportSection } from "@/modules/admin/reports/shared/report-section";
import { StatCard } from "@/modules/admin/reports/shared/stat-card";
import { ReportTable } from "@/modules/admin/reports/tables/report-table";
import type { CancellationsReport } from "@/modules/admin/reports/types";
import { ACTOR_LABELS, cancellationsTable } from "@/modules/admin/reports/views/cancellations/cancellations-table";

const ACTOR_COLORS = { Customer: "var(--chart-1)", Admin: "var(--chart-3)", Unknown: "var(--chart-8)" };

export function CancellationsReportView({ report }: { report: CancellationsReport }) {
  const slices = report.byActor.map((row) => ({
    label: ACTOR_LABELS[row.actor],
    value: row.orders,
    share: row.share,
    color: ACTOR_COLORS[row.actor],
  }));

  return (
    <div className="flex flex-col gap-4">
      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard label="Pedidos cancelados" value={formatValue("count", report.cancelled)} />
        <StatCard label="Tasa de cancelación" value={formatValue("percent", report.rate)} />
        <StatCard label="Ingresos perdidos" value={formatValue("money", report.lostRevenue)} />
      </div>

      <ReportSection title="Quién cancela">
        {slices.length === 0 ? (
          <p className="text-sm text-ink-muted">No hubo cancelaciones en este período.</p>
        ) : (
          <ReportDonutChart items={slices} />
        )}
      </ReportSection>

      <ReportTable table={cancellationsTable(report)} />
    </div>
  );
}
