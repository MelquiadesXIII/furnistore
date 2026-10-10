import type { NextRequest } from "next/server";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { isReportSlug } from "@/modules/admin/reports/definitions";
import { toCsv } from "@/modules/admin/reports/export/csv";
import { adminExportSession, exportError, exportFile } from "@/modules/admin/reports/export/export-response";
import { loadReport, reportTables } from "@/modules/admin/reports/registry";
import { loadReportSearchParams } from "@/modules/admin/reports/search-params";

export const dynamic = "force-dynamic";

export async function GET(request: NextRequest, { params }: { params: Promise<{ report: string }> }) {
  const { report } = await params;
  if (!isReportSlug(report)) return exportError(404, "Ese reporte no existe.");

  const session = await adminExportSession();
  if (!session) return exportError(403, "Necesitas una cuenta de administración para exportar reportes.");

  const result = await loadReport(report, loadReportSearchParams(request.nextUrl.searchParams));
  if (!result.ok) return exportError(result.error.status ?? 502, adminErrorMessage(result.error));

  const tableId = request.nextUrl.searchParams.get("table");
  const table = reportTables(result.value).find((candidate) => candidate.id === tableId);
  if (!table) return exportError(404, "Esa tabla no existe en este reporte.");

  const { period } = result.value.data.meta;

  return exportFile({
    target: report,
    format: "Csv",
    table: table.id,
    from: period.from,
    to: period.to,
    body: toCsv(table),
    contentType: "text/csv; charset=utf-8",
  });
}
