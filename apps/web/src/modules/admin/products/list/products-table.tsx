"use client";

import Image from "next/image";
import Link from "next/link";
import { FURNITURE_MARKS } from "@/components/furniture-marks";
import { Badge } from "@/components/ui/badge";
import { formatMoment } from "@/lib/format-date";
import { formatPrice } from "@/lib/format-price";
import type { AdminProduct } from "@/modules/admin/products/types";
import { DataTable, type Column } from "@/modules/admin/table/data-table";

const LOW_STOCK = 3;

const columns: Column<AdminProduct>[] = [
  {
    id: "name",
    header: "Producto",
    sortKey: "name",
    cell: ({ row }) => {
      const product = row.original;
      const Mark = FURNITURE_MARKS[product.id % FURNITURE_MARKS.length];
      return (
        <Link href={`/admin/products/${product.id}`} className="flex items-center gap-3 hover:text-accent">
          <span className="relative size-10 shrink-0 overflow-hidden rounded-sm bg-surface">
            {product.imageUrl ? (
              <Image src={product.imageUrl} alt="" fill sizes="40px" className="object-cover" />
            ) : (
              <Mark className="h-full w-full p-1.5 text-ink-muted" />
            )}
          </span>
          <span className="flex min-w-0 flex-col">
            <span className="truncate font-medium text-ink">{product.name}</span>
            {product.material && <span className="text-xs text-ink-muted">{product.material}</span>}
          </span>
        </Link>
      );
    },
  },
  {
    id: "category",
    header: "Categoría",
    cell: ({ row }) => <span className="text-ink-muted">{row.original.category.name}</span>,
  },
  {
    id: "price",
    header: "Precio",
    sortKey: "price",
    className: "text-right",
    cell: ({ row }) => <span className="font-mono">{formatPrice(row.original.price)}</span>,
  },
  {
    id: "stock",
    header: "Stock",
    sortKey: "stock",
    className: "text-right",
    cell: ({ row }) => (
      <span className={row.original.stock <= LOW_STOCK ? "font-mono text-brick" : "font-mono"}>
        {row.original.stock}
      </span>
    ),
  },
  {
    id: "status",
    header: "Estado",
    cell: ({ row }) =>
      row.original.isActive ? (
        <Badge variant="outline" className="rounded-sm border-accent text-accent">
          Activo
        </Badge>
      ) : (
        <Badge variant="outline" className="rounded-sm text-ink-muted">
          Archivado
        </Badge>
      ),
  },
  {
    id: "createdAt",
    header: "Creado",
    sortKey: "createdAt",
    className: "whitespace-nowrap",
    cell: ({ row }) => <span className="text-ink-muted">{formatMoment(row.original.createdAt)}</span>,
  },
];

export function ProductsTable({ products }: { products: AdminProduct[] }) {
  return (
    <DataTable columns={columns} data={products} defaultSort="name" emptyMessage="No hay productos con estos filtros." />
  );
}
