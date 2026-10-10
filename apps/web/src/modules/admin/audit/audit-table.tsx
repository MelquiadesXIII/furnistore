"use client";

import Link from "next/link";
import { formatMoment } from "@/lib/format-date";
import { actorLabel, describeChange } from "@/modules/admin/audit/audit-labels";
import { DataTable, type Column } from "@/modules/admin/table/data-table";
import type { AuditEntry } from "@/modules/admin/types";

const ENTITY_LINKS: Record<string, (id: string) => string> = {
  Order: (id) => `/admin/orders/${id}`,
  Product: (id) => `/admin/products/${id}`,
  Customer: (id) => `/admin/customers/${id}`,
  ProductCategory: () => "/admin/categories",
  Report: (id) => `/admin/reports/${id.toLowerCase()}`,
};

const columns: Column<AuditEntry>[] = [
  {
    id: "occurredAt",
    header: "Cuándo",
    className: "whitespace-nowrap align-top",
    cell: ({ row }) => <span className="text-ink-muted">{formatMoment(row.original.occurredAt)}</span>,
  },
  {
    id: "actor",
    header: "Quién",
    className: "align-top",
    cell: ({ row }) => <span className="text-ink">{actorLabel(row.original.actor)}</span>,
  },
  {
    id: "summary",
    header: "Qué pasó",
    className: "w-full min-w-80 align-top whitespace-normal",
    cell: ({ row }) => {
      const entry = row.original;
      const href = ENTITY_LINKS[entry.entityType]?.(entry.entityId);
      const changes = Object.entries(entry.changes);

      return (
        <div className="flex flex-col gap-1">
          {href ? (
            <Link href={href} className="text-ink hover:text-accent">
              {entry.summary}
            </Link>
          ) : (
            <span className="text-ink">{entry.summary}</span>
          )}
          {changes.length > 0 && (
            <span className="line-clamp-2 text-xs break-all text-ink-muted">
              {changes.map(([field, change]) => describeChange(field, change)).join(" · ")}
            </span>
          )}
        </div>
      );
    },
  },
];

export function AuditTable({ entries }: { entries: AuditEntry[] }) {
  return (
    <DataTable columns={columns} data={entries} defaultSort="-occurredAt" emptyMessage="No hay registros con estos filtros." />
  );
}
