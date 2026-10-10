import { defineTable } from "@/modules/admin/reports/tables/table-spec";
import type { CustomersReport } from "@/modules/admin/reports/types";

export function customersTable(report: CustomersReport) {
  return defineTable({
    id: "mejores-clientes",
    title: "Mejores clientes",
    empty: "Nadie compró en este período.",
    rows: report.topCustomers,
    href: (row) => `/admin/customers/${row.customerId}`,
    columns: [
      { header: "Cliente", kind: "text", value: (row) => row.name },
      { header: "Correo", kind: "text", value: (row) => row.email },
      { header: "Pedidos", kind: "count", value: (row) => row.orders },
      { header: "Gastado", kind: "money", value: (row) => row.revenue },
    ],
  });
}
