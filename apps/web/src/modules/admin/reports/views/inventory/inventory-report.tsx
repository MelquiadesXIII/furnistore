import { ReportBarsChart } from "@/modules/admin/reports/charts/bars-chart";
import { formatValue } from "@/modules/admin/reports/format";
import { ReportSection } from "@/modules/admin/reports/shared/report-section";
import { StatCard } from "@/modules/admin/reports/shared/stat-card";
import { ReportTable } from "@/modules/admin/reports/tables/report-table";
import type { InventoryReport } from "@/modules/admin/reports/types";
import { inventoryTable, STOCK_LABELS } from "@/modules/admin/reports/views/inventory/inventory-table";

export function InventoryReportView({ report }: { report: InventoryReport }) {
  const bars = [
    { label: STOCK_LABELS.OutOfStock, value: report.outOfStock, color: "var(--chart-6)" },
    { label: STOCK_LABELS.Low, value: report.lowStock, color: "var(--chart-4)" },
    { label: STOCK_LABELS.Healthy, value: report.healthy, color: "var(--chart-2)" },
  ];

  return (
    <div className="flex flex-col gap-4">
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <StatCard label="Valor del inventario" value={formatValue("money", report.value)} hint="A precio de venta" />
        <StatCard label="Productos activos" value={formatValue("count", report.activeProducts)} />
        <StatCard label="Agotados" value={formatValue("count", report.outOfStock)} />
        <StatCard
          label="Stock bajo"
          value={formatValue("count", report.lowStock)}
          hint={`${report.lowStockThreshold} unidades o menos`}
        />
      </div>

      <ReportSection title="Estado del stock">
        <ReportBarsChart items={bars} kind="count" label="Productos" />
      </ReportSection>

      <ReportTable table={inventoryTable(report)} />
    </div>
  );
}
