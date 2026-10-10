import type { ValueKind } from "@/modules/admin/reports/format";

export type CellKind = ValueKind | "text" | "date" | "moment";

export type CellValue = string | number | null;

type ColumnSpec<T> = {
  header: string;
  kind: CellKind;
  value: (row: T) => CellValue;
};

export type TableSpec<T> = {
  id: string;
  title: string;
  description?: string;
  empty: string;
  columns: ColumnSpec<T>[];
  rows: T[];
  href?: (row: T) => string;
};

export type ReportTableData = {
  id: string;
  title: string;
  description?: string;
  empty: string;
  columns: { header: string; kind: CellKind }[];
  rows: { cells: CellValue[]; href?: string }[];
};

export function defineTable<T>(spec: TableSpec<T>): ReportTableData {
  return {
    id: spec.id,
    title: spec.title,
    description: spec.description,
    empty: spec.empty,
    columns: spec.columns.map(({ header, kind }) => ({ header, kind })),
    rows: spec.rows.map((row) => ({
      cells: spec.columns.map((column) => column.value(row)),
      href: spec.href?.(row),
    })),
  };
}

export function isNumericKind(kind: CellKind): boolean {
  return kind !== "text" && kind !== "date" && kind !== "moment";
}
