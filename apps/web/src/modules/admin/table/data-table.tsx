"use client";

import { flexRender, getCoreRowModel, useReactTable, type ColumnDef } from "@tanstack/react-table";
import { ArrowDown, ArrowUp, ArrowUpDown } from "lucide-react";
import { cn } from "cn";
import { parseAsInteger, parseAsString, useQueryStates } from "nuqs";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useTableTransition } from "@/modules/admin/table/table-frame";

export type Column<T> = ColumnDef<T> & { sortKey?: string; className?: string };

function columnOptions<T>(column: ColumnDef<T>): { sortKey?: string; className?: string } {
  return column as Column<T>;
}

export function DataTable<T>({
  columns,
  data,
  defaultSort,
  emptyMessage,
}: {
  columns: Column<T>[];
  data: T[];
  defaultSort: string;
  emptyMessage: string;
}) {
  const startTransition = useTableTransition();
  const [{ sort }, setParams] = useQueryStates(
    { sort: parseAsString.withDefault(defaultSort), page: parseAsInteger },
    { shallow: false, clearOnDefault: true, startTransition },
  );

  const descending = sort.startsWith("-");
  const activeKey = descending ? sort.slice(1) : sort;

  const table = useReactTable({ data, columns, getCoreRowModel: getCoreRowModel() });

  function toggle(key: string) {
    const nextDescending = key === activeKey ? !descending : false;
    setParams({ sort: `${nextDescending ? "-" : ""}${key}`, page: null });
  }

  return (
    <div className="overflow-x-auto rounded-sm border border-hairline bg-surface-raised">
      <Table>
        <TableHeader>
          {table.getHeaderGroups().map((group) => (
            <TableRow key={group.id} className="hover:bg-transparent">
              {group.headers.map((header) => {
                const meta = columnOptions(header.column.columnDef);
                const sortKey = meta.sortKey;
                const active = sortKey === activeKey;
                const label = header.isPlaceholder
                  ? null
                  : flexRender(header.column.columnDef.header, header.getContext());

                return (
                  <TableHead
                    key={header.id}
                    className={cn("text-xs tracking-wide text-ink-muted uppercase", meta.className)}
                    aria-sort={active ? (descending ? "descending" : "ascending") : undefined}
                  >
                    {sortKey ? (
                      <button
                        type="button"
                        onClick={() => toggle(sortKey)}
                        className={cn(
                          "inline-flex items-center gap-1 uppercase transition-colors hover:text-ink",
                          active && "text-ink",
                        )}
                      >
                        {label}
                        {active ? (
                          descending ? (
                            <ArrowDown className="size-3.5" />
                          ) : (
                            <ArrowUp className="size-3.5" />
                          )
                        ) : (
                          <ArrowUpDown className="size-3.5 opacity-50" />
                        )}
                      </button>
                    ) : (
                      label
                    )}
                  </TableHead>
                );
              })}
            </TableRow>
          ))}
        </TableHeader>
        <TableBody>
          {table.getRowModel().rows.length === 0 ? (
            <TableRow className="hover:bg-transparent">
              <TableCell colSpan={columns.length} className="py-16 text-center text-ink-muted">
                {emptyMessage}
              </TableCell>
            </TableRow>
          ) : (
            table.getRowModel().rows.map((row) => (
              <TableRow key={row.id}>
                {row.getVisibleCells().map((cell) => (
                  <TableCell key={cell.id} className={columnOptions(cell.column.columnDef).className}>
                    {flexRender(cell.column.columnDef.cell, cell.getContext())}
                  </TableCell>
                ))}
              </TableRow>
            ))
          )}
        </TableBody>
      </Table>
    </div>
  );
}
