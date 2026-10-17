import { STORE_TIME_ZONE } from "@/lib/format-date";
import type { CellKind, CellValue, ReportTableData } from "@/modules/admin/reports/tables/table-spec";

const MOMENT = new Intl.DateTimeFormat("sv-SE", {
  timeZone: STORE_TIME_ZONE,
  year: "numeric",
  month: "2-digit",
  day: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
});

const UNITS: Partial<Record<CellKind, string>> = {
  money: "USD",
  percent: "%",
};

function header(column: { header: string; kind: CellKind }): string {
  const unit = UNITS[column.kind];
  return unit && !column.header.includes(unit) ? `${column.header} (${unit})` : column.header;
}

function raw(kind: CellKind, value: CellValue): string {
  if (value === null || value === "") return "";
  if (kind === "moment") return MOMENT.format(new Date(String(value)));
  if (kind === "percent") return (Number(value) * 100).toFixed(1);
  return String(value);
}

function escape(field: string): string {
  return /[",;\r\n]/.test(field) ? `"${field.replace(/"/g, '""')}"` : field;
}

export function toCsv(table: ReportTableData): string {
  const lines = [
    table.columns.map((column) => escape(header(column))).join(","),
    ...table.rows.map((row) =>
      row.cells.map((cell, index) => escape(raw(table.columns[index].kind, cell))).join(","),
    ),
  ];
  return `﻿${lines.join("\r\n")}\r\n`;
}
