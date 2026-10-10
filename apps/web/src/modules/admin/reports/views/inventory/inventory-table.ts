import { defineTable } from "@/modules/admin/reports/tables/table-spec";
import type { InventoryReport } from "@/modules/admin/reports/types";

export const STOCK_LABELS = {
  OutOfStock: "Agotado",
  Low: "Stock bajo",
  Healthy: "Bien",
};

export function inventoryTable(report: InventoryReport) {
  return defineTable({
    id: "stock",
    title: "Stock por producto",
    empty: "No hay productos activos.",
    rows: report.products,
    href: (row) => `/admin/products/${row.productId}`,
    columns: [
      { header: "Producto", kind: "text", value: (row) => row.name },
      { header: "Categoría", kind: "text", value: (row) => row.category },
      { header: "Stock", kind: "count", value: (row) => row.stock },
      { header: "Precio", kind: "money", value: (row) => row.price },
      { header: "Valor", kind: "money", value: (row) => row.value },
      { header: "Estado", kind: "text", value: (row) => STOCK_LABELS[row.level] },
    ],
  });
}
