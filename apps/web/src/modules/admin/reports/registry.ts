import "server-only";

import { ok, type Result } from "@/lib/result";
import { getReport, type ReportData } from "@/modules/admin/reports/api";
import type { ReportSlug } from "@/modules/admin/reports/definitions";
import { tablesOf, type ReportBlock } from "@/modules/admin/reports/document";
import type { ReportTableData } from "@/modules/admin/reports/tables/table-spec";
import type { ReportQuery } from "@/modules/admin/reports/types";
import { cancellationsDocument } from "@/modules/admin/reports/views/cancellations/cancellations-document";
import { customersDocument } from "@/modules/admin/reports/views/customers/customers-document";
import { inventoryDocument } from "@/modules/admin/reports/views/inventory/inventory-document";
import { operationsDocument } from "@/modules/admin/reports/views/operations/operations-document";
import { productsDocument } from "@/modules/admin/reports/views/products/products-document";
import { salesDocument } from "@/modules/admin/reports/views/sales/sales-document";

export type LoadedReport = { [S in ReportSlug]: { slug: S; data: ReportData[S] } }[ReportSlug];

export async function loadReport(slug: ReportSlug, query: ReportQuery): Promise<Result<LoadedReport>> {
  const result = await getReport(slug, query);
  return result.ok ? ok({ slug, data: result.value } as LoadedReport) : result;
}

export function reportDocument(report: LoadedReport): ReportBlock[] {
  switch (report.slug) {
    case "sales":
      return salesDocument(report.data);
    case "products":
      return productsDocument(report.data);
    case "operations":
      return operationsDocument(report.data);
    case "cancellations":
      return cancellationsDocument(report.data);
    case "inventory":
      return inventoryDocument(report.data);
    case "customers":
      return customersDocument(report.data);
  }
}

export function reportTables(report: LoadedReport): ReportTableData[] {
  return tablesOf(reportDocument(report));
}
