"use client";

import Link from "next/link";
import { CustomerBadges } from "@/modules/admin/customers/customer-badges";
import type { AdminCustomerSummary } from "@/modules/admin/customers/types";
import { DataTable, type Column } from "@/modules/admin/table/data-table";

const columns: Column<AdminCustomerSummary>[] = [
  {
    id: "name",
    header: "Cliente",
    sortKey: "name",
    cell: ({ row }) => (
      <Link href={`/admin/customers/${row.original.id}`} className="font-medium text-ink hover:text-accent">
        {row.original.firstName} {row.original.lastName}
      </Link>
    ),
  },
  {
    id: "email",
    header: "Correo",
    sortKey: "email",
    cell: ({ row }) => <span className="text-ink-muted">{row.original.email}</span>,
  },
  {
    id: "phone",
    header: "Teléfono",
    cell: ({ row }) => <span className="text-ink-muted">{row.original.phone ?? "—"}</span>,
  },
  {
    id: "orders",
    header: "Pedidos",
    sortKey: "orders",
    className: "text-right",
    cell: ({ row }) => (
      <Link
        href={`/admin/orders?q=${encodeURIComponent(row.original.email)}`}
        className="font-mono hover:text-accent"
      >
        {row.original.orderCount}
      </Link>
    ),
  },
  {
    id: "status",
    header: "Cuenta",
    cell: ({ row }) => <CustomerBadges {...row.original} />,
  },
];

export function CustomersTable({ customers }: { customers: AdminCustomerSummary[] }) {
  return (
    <DataTable columns={columns} data={customers} defaultSort="name" emptyMessage="No hay clientes con estos filtros." />
  );
}
