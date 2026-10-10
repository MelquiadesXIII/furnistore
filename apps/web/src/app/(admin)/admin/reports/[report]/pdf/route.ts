import type { NextRequest } from "next/server";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { exportTitle, isExportTarget, REPORT_SLUGS, REPORTS } from "@/modules/admin/reports/definitions";
import { adminExportSession, exportError, exportFile } from "@/modules/admin/reports/export/export-response";
import { renderReportPdf } from "@/modules/admin/reports/pdf/report-pdf";
import { loadReport, reportDocument } from "@/modules/admin/reports/registry";
import { loadReportSearchParams } from "@/modules/admin/reports/search-params";

export const dynamic = "force-dynamic";

export async function GET(request: NextRequest, { params }: { params: Promise<{ report: string }> }) {
  const { report } = await params;
  if (!isExportTarget(report)) return exportError(404, "Ese reporte no existe.");

  const session = await adminExportSession();
  if (!session) return exportError(403, "Necesitas una cuenta de administración para exportar reportes.");

  const query = loadReportSearchParams(request.nextUrl.searchParams);
  const slugs = report === "all" ? REPORT_SLUGS : [report];
  const results = await Promise.all(slugs.map((slug) => loadReport(slug, query)));

  const failure = results.find((result) => !result.ok);
  if (failure && !failure.ok) {
    return exportError(failure.error.status ?? 502, adminErrorMessage(failure.error));
  }

  const loaded = results.flatMap((result) => (result.ok ? [result.value] : []));
  const meta = loaded[0].data.meta;

  const pdf = await renderReportPdf({
    title: exportTitle(report),
    meta,
    generatedBy: session.email,
    showGrouping: slugs.some((slug) => REPORTS[slug].hasSeries),
    sections: loaded.map((entry) => ({
      title: REPORTS[entry.slug].title,
      summary: REPORTS[entry.slug].summary,
      blocks: reportDocument(entry),
    })),
  });

  return exportFile({
    target: report,
    format: "Pdf",
    from: meta.period.from,
    to: meta.period.to,
    body: new Uint8Array(pdf),
    contentType: "application/pdf",
  });
}
