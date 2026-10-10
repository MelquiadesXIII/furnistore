import type { ApiSchemas } from "@/lib/api/contract";
import { seriesTone, type ReportBlock, type Tone } from "@/modules/admin/reports/document";
import { formatValue } from "@/modules/admin/reports/format";
import type { InventoryReport } from "@/modules/admin/reports/types";
import { inventoryTables, STOCK_LEVEL_LABELS } from "@/modules/admin/reports/views/inventory/inventory-tables";

const LEVEL_TONES: Record<ApiSchemas["StockLevel"], Tone> = {
  OutOfStock: "chart-6",
  Low: "chart-4",
  Healthy: "chart-2",
};

const SOONEST = 10;

export function inventoryDocument(report: InventoryReport): ReportBlock[] {
  const { summary } = report;
  const levels = { OutOfStock: summary.outOfStock, Low: summary.lowStock, Healthy: summary.healthy };
  const runningOut = report.products
    .filter((row) => row.daysOfCover !== null && row.stock > 0)
    .sort((a, b) => (a.daysOfCover ?? 0) - (b.daysOfCover ?? 0))
    .slice(0, SOONEST);
  const valued = report.categories.filter((row) => row.value > 0);

  const blocks: ReportBlock[] = [
    {
      type: "cards",
      columns: 4,
      cards: [
        {
          type: "stat",
          label: "Valor del inventario",
          value: formatValue("money", summary.value),
          hint: `${formatValue("count", summary.units)} unidades a precio de venta`,
        },
        { type: "stat", label: "Productos activos", value: formatValue("count", summary.activeProducts) },
        { type: "stat", label: "Agotados", value: formatValue("count", summary.outOfStock), hint: "Stock en cero" },
        { type: "stat", label: "En riesgo", value: formatValue("count", summary.lowStock), hint: "Reponer pronto" },
      ],
    },
    {
      type: "pair",
      blocks: [
        {
          type: "chart",
          title: "Estado del stock",
          empty: "No hay productos activos.",
          chart:
            summary.activeProducts === 0
              ? null
              : {
                  kind: "bars",
                  layout: "vertical",
                  valueKind: "count",
                  valueLabel: "Productos",
                  items: (["OutOfStock", "Low", "Healthy"] as const).map((level) => ({
                    label: STOCK_LEVEL_LABELS[level],
                    value: levels[level],
                    tone: LEVEL_TONES[level],
                  })),
                },
        },
        {
          type: "chart",
          title: "Valor por categoría",
          empty: "No hay stock valorizado.",
          chart:
            valued.length === 0
              ? null
              : {
                  kind: "donut",
                  valueKind: "money",
                  items: valued.map((row, index) => ({
                    label: row.name,
                    value: row.value,
                    share: row.share,
                    tone: seriesTone(index),
                  })),
                },
        },
      ],
    },
  ];

  if (runningOut.length > 0) {
    blocks.push({
      type: "chart",
      title: "Lo que se agota primero",
      description: "Días de stock que quedan al ritmo de venta del período elegido.",
      chart: {
        kind: "bars",
        layout: "horizontal",
        valueKind: "days",
        valueLabel: "Cobertura",
        items: runningOut.map((row) => ({ label: row.name, value: row.daysOfCover ?? 0, tone: LEVEL_TONES[row.level] })),
      },
    });
  }

  return [...blocks, ...inventoryTables(report).map((table) => ({ type: "table" as const, table }))];
}
