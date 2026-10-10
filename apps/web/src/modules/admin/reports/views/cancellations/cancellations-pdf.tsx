import { formatValue } from "@/modules/admin/reports/format";
import { PdfDonutChart } from "@/modules/admin/reports/pdf/pdf-charts";
import { PdfChartBox, PdfStats, PdfTable } from "@/modules/admin/reports/pdf/pdf-layout";
import { PDF_COLORS } from "@/modules/admin/reports/pdf/theme";
import type { CancellationsReport } from "@/modules/admin/reports/types";
import { ACTOR_LABELS, cancellationsTable } from "@/modules/admin/reports/views/cancellations/cancellations-table";

const ACTOR_COLORS = { Customer: PDF_COLORS.accent, Admin: PDF_COLORS.blue, Unknown: PDF_COLORS.muted };

export function CancellationsPdf({ report }: { report: CancellationsReport }) {
  const slices = report.byActor.map((row) => ({
    label: ACTOR_LABELS[row.actor],
    value: row.orders,
    share: row.share,
    color: ACTOR_COLORS[row.actor],
  }));

  return (
    <>
      <PdfStats
        stats={[
          { label: "Pedidos cancelados", value: formatValue("count", report.cancelled) },
          { label: "Tasa de cancelación", value: formatValue("percent", report.rate) },
          { label: "Ingresos perdidos", value: formatValue("money", report.lostRevenue) },
        ]}
      />
      <PdfChartBox title="Quién cancela" empty={slices.length === 0 ? "No hubo cancelaciones en este período." : undefined}>
        <PdfDonutChart items={slices} />
      </PdfChartBox>
      <PdfTable table={cancellationsTable(report)} />
    </>
  );
}
