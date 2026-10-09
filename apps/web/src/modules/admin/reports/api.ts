import "server-only";

import { authedApiFetch } from "@/lib/api/authed";
import type { ApiSchemas } from "@/lib/api/contract";
import { toQueryString } from "@/lib/api/query";
import type { Result } from "@/lib/result";
import type { ReportSlug } from "@/modules/admin/reports/definitions";
import type {
  CancellationsReport,
  CustomersReport,
  InventoryReport,
  OperationsReport,
  ProductsReport,
  ReportFormat,
  ReportKind,
  ReportQuery,
  SalesReport,
} from "@/modules/admin/reports/types";

export type ReportData = {
  sales: SalesReport;
  products: ProductsReport;
  operations: OperationsReport;
  cancellations: CancellationsReport;
  inventory: InventoryReport;
  customers: CustomersReport;
};

const REPORT_TIMEOUT_MS = 15000;

export function getReport<S extends ReportSlug>(
  slug: S,
  query: ReportQuery,
): Promise<Result<ReportData[S]>> {
  return authedApiFetch<ReportData[S]>(
    `/api/admin/reports/${slug}${toQueryString({
      from: query.from,
      to: query.to,
      groupBy: query.groupBy,
    })}`,
    { timeoutMs: REPORT_TIMEOUT_MS },
  );
}

export function recordReportExport(input: {
  report: ReportKind;
  format: ReportFormat;
  table?: string;
  from: string;
  to: string;
}): Promise<Result<void>> {
  return authedApiFetch<void>("/api/admin/reports/exports", {
    method: "POST",
    body: {
      report: input.report,
      format: input.format,
      table: input.table ?? null,
      from: input.from,
      to: input.to,
    } satisfies ApiSchemas["ReportExportRequest"],
  });
}
