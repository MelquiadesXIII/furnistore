import type { ApiSchemas } from "@/lib/api/contract";
import { defineTable, type ReportTableData } from "@/modules/admin/reports/tables/table-spec";
import type { InventoryReport } from "@/modules/admin/reports/types";

export const STOCK_LEVEL_LABELS: Record<ApiSchemas["StockLevel"], string> = {
  OutOfStock: "Agotado",
  Low: "En riesgo",
  Healthy: "Bien",
};

export function inventoryTables(report: InventoryReport): ReportTableData[] {
  const { lowStockThreshold, coverAlertDays } = report.summary;

  return [
    defineTable({
      id: "productos",
      title: "Stock por producto",
      description: `En riesgo: ${lowStockThreshold} unidades o menos, o stock para ${coverAlertDays} días o menos al ritmo de venta del período. Primero lo más urgente.`,
      empty: "No hay productos activos.",
      rows: report.products,
      href: (row) => `/admin/products/${row.productId}`,
      columns: [
        { header: "Producto", kind: "text", value: (row) => row.name },
        { header: "Categoría", kind: "text", value: (row) => row.category },
        { header: "Estado", kind: "text", value: (row) => STOCK_LEVEL_LABELS[row.level] },
        { header: "Stock", kind: "count", value: (row) => row.stock },
        { header: "Vendidas", kind: "count", value: (row) => row.unitsSold },
        { header: "Venta diaria", kind: "decimal", value: (row) => row.dailyUnits },
        { header: "Cobertura", kind: "days", value: (row) => row.daysOfCover },
        { header: "Valor", kind: "money", value: (row) => row.value },
      ],
    }),
    defineTable({
      id: "categorias",
      title: "Inventario por categoría",
      description: "Valor a precio de venta de los productos activos.",
      empty: "No hay productos activos.",
      rows: report.categories,
      columns: [
        { header: "Categoría", kind: "text", value: (row) => row.name },
        { header: "Productos", kind: "count", value: (row) => row.products },
        { header: "Unidades", kind: "count", value: (row) => row.units },
        { header: "Valor", kind: "money", value: (row) => row.value },
        { header: "% del valor", kind: "percent", value: (row) => row.share },
      ],
    }),
  ];
}
