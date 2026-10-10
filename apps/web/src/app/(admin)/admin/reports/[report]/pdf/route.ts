import type { NextRequest } from "next/server";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { isReportSlug } from "@/modules/admin/reports/definitions";
import { adminExportSession, exportError, exportFile } from "@/modules/admin/reports/export/export-response";
import { renderReportPdf } from "@/modules/admin/reports/pdf/render-report-pdf";
import { loadReport } from "@/modules/admin/reports/registry";
import { loadReportSearchParams } from "@/modules/admin/reports/search-params";

export const dynamic = "force-dynamic";

export async function GET(request: NextRequest, { params }: { params: Promise<{ report: string }> }) {
  const { report } = await params;
  if (!isReportSlug(report)) return exportError(404, "Ese reporte no existe.");

  const session = await adminExportSession();
  if (!session) return exportError(403, "Necesitas una cuenta de administración para exportar reportes.");

  const result = await loadReport(report, loadReportSearchParams(request.nextUrl.searchParams));
  if (!result.ok) return exportError(result.error.status ?? 502, adminErrorMessage(result.error));

  const pdf = await renderReportPdf(result.value, session.email);
  const { period } = result.value.data.meta;

  return exportFile({
    slug: report,
    format: "Pdf",
    from: period.from,
    to: period.to,
    body: new Uint8Array(pdf),
    contentType: "application/pdf",
  });
}
