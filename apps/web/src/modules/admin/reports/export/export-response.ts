import "server-only";

import { NextResponse } from "next/server";
import { getSession, getSessionUser } from "@/lib/session";
import { recordReportExport } from "@/modules/admin/reports/api";
import { exportFileName, exportKind, type ExportTarget } from "@/modules/admin/reports/definitions";
import type { ReportFormat } from "@/modules/admin/reports/types";

export async function adminExportSession(): Promise<{ token: string; email: string } | null> {
  const [user, token] = await Promise.all([getSessionUser(), getSession()]);
  return user?.isAdmin && token ? { token, email: user.email } : null;
}

export function exportError(status: number, message: string): NextResponse {
  return NextResponse.json({ message }, { status, headers: { "Cache-Control": "no-store" } });
}

export async function exportFile(input: {
  target: ExportTarget;
  format: ReportFormat;
  table?: string;
  from: string;
  to: string;
  body: Uint8Array<ArrayBuffer> | string;
  contentType: string;
}): Promise<NextResponse> {
  const recorded = await recordReportExport({
    report: exportKind(input.target),
    format: input.format,
    table: input.table,
    from: input.from,
    to: input.to,
  });
  if (!recorded.ok) {
    console.error(`[reports] No se pudo registrar la exportación de ${input.target}`, recorded.error);
  }

  const extension = input.format === "Pdf" ? "pdf" : "csv";
  const parts = ["furnistore", exportFileName(input.target), input.table, `${input.from}_${input.to}`];
  const fileName = `${parts.filter(Boolean).join("-")}.${extension}`;

  return new NextResponse(input.body, {
    headers: {
      "Content-Type": input.contentType,
      "Content-Disposition": `attachment; filename="${fileName}"`,
      "Cache-Control": "no-store",
    },
  });
}
