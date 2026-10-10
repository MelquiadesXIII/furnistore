import { defineTable, type ReportTableData } from "@/modules/admin/reports/tables/table-spec";
import type { CustomersReport } from "@/modules/admin/reports/types";

export function customersTables(report: CustomersReport): ReportTableData[] {
  return [
    defineTable({
      id: "mejores-clientes",
      title: "Mejores clientes",
      description: "Los 10 que más compraron en el período.",
      empty: "Nadie compró en este período.",
      rows: report.topCustomers,
      href: (row) => `/admin/customers/${row.customerId}`,
      columns: [
        { header: "Cliente", kind: "text", value: (row) => row.name },
        { header: "Correo", kind: "text", value: (row) => row.email },
        { header: "Pedidos", kind: "count", value: (row) => row.orders },
        { header: "Gastado", kind: "money", value: (row) => row.revenue },
        { header: "% de ventas", kind: "percent", value: (row) => row.share },
        { header: "Última compra", kind: "moment", value: (row) => row.lastOrderAt },
      ],
    }),
    defineTable({
      id: "provincias",
      title: "Ventas por provincia",
      description: "Según la dirección de entrega de cada pedido.",
      empty: "No hubo ventas en este período.",
      rows: report.provinces,
      columns: [
        { header: "Provincia", kind: "text", value: (row) => row.name },
        { header: "Pedidos", kind: "count", value: (row) => row.orders },
        { header: "Compradores", kind: "count", value: (row) => row.buyers },
        { header: "Ingresos", kind: "money", value: (row) => row.revenue },
        { header: "% de ventas", kind: "percent", value: (row) => row.share },
      ],
    }),
    defineTable({
      id: "ciudades",
      title: "Ciudades con más ventas",
      description: "Las 15 primeras.",
      empty: "No hubo ventas en este período.",
      rows: report.cities,
      columns: [
        { header: "Ciudad", kind: "text", value: (row) => row.name },
        { header: "Provincia", kind: "text", value: (row) => row.province },
        { header: "Pedidos", kind: "count", value: (row) => row.orders },
        { header: "Compradores", kind: "count", value: (row) => row.buyers },
        { header: "Ingresos", kind: "money", value: (row) => row.revenue },
        { header: "% de ventas", kind: "percent", value: (row) => row.share },
      ],
    }),
  ];
}
