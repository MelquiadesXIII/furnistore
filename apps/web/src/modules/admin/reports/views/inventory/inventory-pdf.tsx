import { formatValue } from "@/modules/admin/reports/format";
import { PdfBarsChart } from "@/modules/admin/reports/pdf/pdf-charts";
import { CHART_WIDTH, PdfChartBox, PdfStats, PdfTable } from "@/modules/admin/reports/pdf/pdf-layout";
import { PDF_COLORS } from "@/modules/admin/reports/pdf/theme";
import type { InventoryReport } from "@/modules/admin/reports/types";
import { inventoryTable, STOCK_LABELS } from "@/modules/admin/reports/views/inventory/inventory-table";

export function InventoryPdf({ report }: { report: InventoryReport }) {
  const bars = [
    { label: STOCK_LABELS.OutOfStock, value: report.outOfStock, color: PDF_COLORS.red },
    { label: STOCK_LABELS.Low, value: report.lowStock, color: PDF_COLORS.orange },
    { label: STOCK_LABELS.Healthy, value: report.healthy, color: PDF_COLORS.green },
  ];

  return (
    <>
      <PdfStats
        stats={[
          { label: "Valor del inventario", value: formatValue("money", report.value) },
          { label: "Productos activos", value: formatValue("count", report.activeProducts) },
          { label: "Agotados", value: formatValue("count", report.outOfStock) },
          { label: "Stock bajo", value: formatValue("count", report.lowStock) },
        ]}
      />
      <PdfChartBox title="Estado del stock">
        <PdfBarsChart items={bars} kind="count" width={CHART_WIDTH} />
      </PdfChartBox>
      <PdfTable table={inventoryTable(report)} />
    </>
  );
}
