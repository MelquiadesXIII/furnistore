import { formatBucket, formatValue } from "@/modules/admin/reports/format";
import { PdfAreaChart } from "@/modules/admin/reports/pdf/pdf-charts";
import { CHART_WIDTH, PdfChartBox, PdfStats, PdfTable } from "@/modules/admin/reports/pdf/pdf-layout";
import type { SalesReport } from "@/modules/admin/reports/types";
import { salesTable } from "@/modules/admin/reports/views/sales/sales-table";

export function SalesPdf({ report }: { report: SalesReport }) {
  const points = report.series.map((point) => ({
    label: formatBucket(point.start, point.end, report.meta.groupBy),
    value: point.revenue,
  }));

  return (
    <>
      <PdfStats
        stats={[
          { label: "Ingresos", value: formatValue("money", report.revenue) },
          { label: "Pedidos", value: formatValue("count", report.orders) },
          { label: "Ticket promedio", value: formatValue("money", report.averageOrderValue) },
          { label: "Unidades vendidas", value: formatValue("count", report.units) },
        ]}
      />
      <PdfChartBox title="Ingresos en el tiempo">
        <PdfAreaChart points={points} kind="money" width={CHART_WIDTH} />
      </PdfChartBox>
      <PdfTable table={salesTable(report)} />
    </>
  );
}
