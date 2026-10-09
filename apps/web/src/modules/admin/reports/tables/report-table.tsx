import { cn } from "cn";
import Link from "next/link";
import { formatCalendarDate, formatMoment } from "@/lib/format-date";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { formatValue } from "@/modules/admin/reports/format";
import { ReportSection } from "@/modules/admin/reports/shared/report-section";
import {
  isNumericKind,
  type CellKind,
  type CellValue,
  type ReportTableData,
} from "@/modules/admin/reports/tables/table-spec";

function formatCell(kind: CellKind, value: CellValue): string {
  if (value === null || value === "") return "—";
  if (kind === "text") return String(value);
  if (kind === "date") return formatCalendarDate(String(value));
  if (kind === "moment") return formatMoment(String(value));
  return formatValue(kind, Number(value));
}

const EDGES = "first:pl-5 last:pr-5";

export function ReportTable({ table }: { table: ReportTableData }) {
  return (
    <ReportSection title={table.title} description={table.description} flush>
      {table.rows.length === 0 ? (
        <p className="px-5 pb-5 text-sm text-ink-muted">{table.empty}</p>
      ) : (
        <div className="overflow-x-auto">
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                {table.columns.map((column) => (
                  <TableHead
                    key={column.header}
                    className={cn(
                      "text-xs tracking-wide text-ink-muted uppercase",
                      EDGES,
                      isNumericKind(column.kind) && "text-right",
                    )}
                  >
                    {column.header}
                  </TableHead>
                ))}
              </TableRow>
            </TableHeader>
            <TableBody>
              {table.rows.map((row, rowIndex) => (
                <TableRow key={rowIndex}>
                  {row.cells.map((cell, cellIndex) => {
                    const kind = table.columns[cellIndex].kind;
                    const text = formatCell(kind, cell);

                    return (
                      <TableCell
                        key={cellIndex}
                        className={cn(
                          EDGES,
                          isNumericKind(kind) && "text-right font-mono tabular-nums",
                        )}
                      >
                        {cellIndex === 0 && row.href ? (
                          <Link href={row.href} className="text-ink hover:text-accent hover:underline">
                            {text}
                          </Link>
                        ) : (
                          text
                        )}
                      </TableCell>
                    );
                  })}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </ReportSection>
  );
}
