import "server-only";

import { ok, type Result } from "@/lib/result";
import { getReport, type ReportData } from "@/modules/admin/reports/api";
import type { ReportSlug } from "@/modules/admin/reports/definitions";
import type { ReportTableData } from "@/modules/admin/reports/tables/table-spec";
import type { ReportQuery } from "@/modules/admin/reports/types";
import { cancellationsTable } from "@/modules/admin/reports/views/cancellations/cancellations-table";
import { customersTable } from "@/modules/admin/reports/views/customers/customers-table";
import { inventoryTable } from "@/modules/admin/reports/views/inventory/inventory-table";
import { operationsTable } from "@/modules/admin/reports/views/operations/operations-table";
import { productsTable } from "@/modules/admin/reports/views/products/products-table";
import { salesTable } from "@/modules/admin/reports/views/sales/sales-table";

export type LoadedReport = { [S in ReportSlug]: { slug: S; data: ReportData[S] } }[ReportSlug];

export async function loadReport(slug: ReportSlug, query: ReportQuery): Promise<Result<LoadedReport>> {
  const result = await getReport(slug, query);
  return result.ok ? ok({ slug, data: result.value } as LoadedReport) : result;
}

export function reportTable(report: LoadedReport): ReportTableData {
  switch (report.slug) {
    case "sales":
      return salesTable(report.data);
    case "products":
      return productsTable(report.data);
    case "operations":
      return operationsTable(report.data);
    case "cancellations":
      return cancellationsTable(report.data);
    case "inventory":
      return inventoryTable(report.data);
    case "customers":
      return customersTable(report.data);
  }
}
