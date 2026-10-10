import { defineTable, type ReportTableData } from "@/modules/admin/reports/tables/table-spec";
import type { ProductsReport } from "@/modules/admin/reports/types";

export function productsTables(report: ProductsReport): ReportTableData[] {
  return [
    defineTable({
      id: "productos",
      title: "Ventas por producto",
      description: "Ingresos por productos, sin envío. Ordenado de mayor a menor.",
      empty: "No se vendió ningún producto en este período.",
      rows: report.products,
      href: (row) => `/admin/products/${row.productId}`,
      columns: [
        { header: "Producto", kind: "text", value: (row) => row.name },
        { header: "Categoría", kind: "text", value: (row) => row.category },
        { header: "Unidades", kind: "count", value: (row) => row.units },
        { header: "Pedidos", kind: "count", value: (row) => row.orders },
        { header: "Ingresos", kind: "money", value: (row) => row.revenue },
        { header: "% del total", kind: "percent", value: (row) => row.share },
        { header: "Precio promedio", kind: "money", value: (row) => row.averageUnitPrice },
      ],
    }),
    defineTable({
      id: "categorias",
      title: "Ventas por categoría",
      empty: "Todavía no hay categorías.",
      rows: report.categories,
      columns: [
        { header: "Categoría", kind: "text", value: (row) => row.name },
        { header: "Unidades", kind: "count", value: (row) => row.units },
        { header: "Pedidos", kind: "count", value: (row) => row.orders },
        { header: "Ingresos", kind: "money", value: (row) => row.revenue },
        { header: "% del total", kind: "percent", value: (row) => row.share },
      ],
    }),
    defineTable({
      id: "sin-ventas",
      title: "Productos activos sin ventas",
      description: "Muebles a la venta que no tuvieron ningún pedido en el período. Ordenado por stock.",
      empty: "Todos los productos activos se vendieron al menos una vez.",
      rows: report.unsold,
      href: (row) => `/admin/products/${row.productId}`,
      columns: [
        { header: "Producto", kind: "text", value: (row) => row.name },
        { header: "Categoría", kind: "text", value: (row) => row.category },
        { header: "Precio", kind: "money", value: (row) => row.price },
        { header: "Stock", kind: "count", value: (row) => row.stock },
        { header: "Última venta", kind: "moment", value: (row) => row.lastSoldAt },
      ],
    }),
  ];
}
