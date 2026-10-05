"use client";

import Link from "next/link";
import { CategoryRowActions } from "@/modules/admin/categories/category-row-actions";
import type { AdminCategory } from "@/modules/admin/categories/types";
import { DataTable, type Column } from "@/modules/admin/table/data-table";

const columns: Column<AdminCategory>[] = [
  {
    id: "name",
    header: "Nombre",
    sortKey: "name",
    cell: ({ row }) => <span className="font-medium text-ink">{row.original.name}</span>,
  },
  {
    id: "productCount",
    header: "Productos",
    sortKey: "productCount",
    cell: ({ row }) => (
      <Link
        href={`/admin/products?categoryId=${row.original.id}`}
        className="font-mono text-ink-muted hover:text-accent"
      >
        {row.original.activeProductCount} activos / {row.original.productCount}
      </Link>
    ),
  },
  {
    id: "actions",
    header: () => <span className="sr-only">Acciones</span>,
    className: "text-right",
    cell: ({ row }) => <CategoryRowActions category={row.original} />,
  },
];

export function CategoriesTable({ categories }: { categories: AdminCategory[] }) {
  return (
    <DataTable columns={columns} data={categories} defaultSort="name" emptyMessage="No hay categorías." />
  );
}
