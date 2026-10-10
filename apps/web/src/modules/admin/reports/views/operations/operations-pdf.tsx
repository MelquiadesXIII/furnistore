import { formatValue } from "@/modules/admin/reports/format";
import { PdfBarsChart } from "@/modules/admin/reports/pdf/pdf-charts";
import { CHART_WIDTH, PdfChartBox, PdfStats, PdfTable } from "@/modules/admin/reports/pdf/pdf-layout";
import { PDF_COLORS } from "@/modules/admin/reports/pdf/theme";
import type { OperationsReport } from "@/modules/admin/reports/types";
import { operationsTable } from "@/modules/admin/reports/views/operations/operations-table";
import { ORDER_STATUS_LABELS } from "@/modules/orders/order-status";

const STATUS_COLORS = {
  Paid: PDF_COLORS.accent,
  Processing: PDF_COLORS.orange,
  Shipped: PDF_COLORS.blue,
  Delivered: PDF_COLORS.green,
  Cancelled: PDF_COLORS.red,
};

export function OperationsPdf({ report }: { report: OperationsReport }) {
  const bars = report.statuses.map((row) => ({
    label: ORDER_STATUS_LABELS[row.status],
    value: row.orders,
    color: STATUS_COLORS[row.status],
  }));

  return (
    <>
      <PdfStats
        stats={[
          { label: "Pedidos del período", value: formatValue("count", report.orders) },
          { label: "Entregados a tiempo", value: formatValue("percent", report.onTimeRate) },
          { label: "Atrasados ahora", value: formatValue("count", report.overdue.length) },
        ]}
      />
      <PdfChartBox title="Pedidos por estado">
        <PdfBarsChart items={bars} kind="count" width={CHART_WIDTH} />
      </PdfChartBox>
      <PdfTable table={operationsTable(report)} />
    </>
  );
}
