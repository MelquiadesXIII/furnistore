import { defineTable } from "@/modules/admin/reports/tables/table-spec";
import type { ProductsReport } from "@/modules/admin/reports/types";

export function productsTable(report: ProductsReport) {
  return defineTable({
    id: "productos",
    title: "Ventas por producto",
    empty: "No se vendió ningún producto en este período.",
    rows: report.products,
    href: (row) => `/admin/products/${row.productId}`,
    columns: [
      { header: "Producto", kind: "text", value: (row) => row.name },
      { header: "Categoría", kind: "text", value: (row) => row.category },
      { header: "Unidades", kind: "count", value: (row) => row.units },
      { header: "Ingresos", kind: "money", value: (row) => row.revenue },
      { header: "% del total", kind: "percent", value: (row) => row.share },
    ],
  });
}
