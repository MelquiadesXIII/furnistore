import { formatValue } from "@/modules/admin/reports/format";
import { PdfBarsChart } from "@/modules/admin/reports/pdf/pdf-charts";
import { CHART_WIDTH, PdfChartBox, PdfStats, PdfTable } from "@/modules/admin/reports/pdf/pdf-layout";
import type { ProductsReport } from "@/modules/admin/reports/types";
import { productsTable } from "@/modules/admin/reports/views/products/products-table";

export function ProductsPdf({ report }: { report: ProductsReport }) {
  const top = report.products.slice(0, 10).map((row) => ({ label: row.name, value: row.revenue }));

  return (
    <>
      <PdfStats
        stats={[
          { label: "Ingresos por productos", value: formatValue("money", report.revenue) },
          { label: "Productos vendidos", value: formatValue("count", report.products.length) },
          { label: "Activos sin ventas", value: formatValue("count", report.unsoldProducts) },
        ]}
      />
      <PdfChartBox title="Top 10 por ingresos" empty={top.length === 0 ? "No hubo ventas en este período." : undefined}>
        <PdfBarsChart items={top} kind="money" width={CHART_WIDTH} horizontal />
      </PdfChartBox>
      <PdfTable table={productsTable(report)} />
    </>
  );
}
