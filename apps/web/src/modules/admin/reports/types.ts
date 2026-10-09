import type { ApiSchemas } from "@/lib/api/contract";

export type ReportMeta = ApiSchemas["ReportMeta"];
export type ReportGrouping = ApiSchemas["ReportGrouping"];
export type ReportKind = ApiSchemas["ReportKind"];
export type ReportFormat = ApiSchemas["ReportFormat"];

export type SalesReport = ApiSchemas["SalesReport"];
export type ProductsReport = ApiSchemas["ProductsReport"];
export type OperationsReport = ApiSchemas["OperationsReport"];
export type CancellationsReport = ApiSchemas["CancellationsReport"];
export type InventoryReport = ApiSchemas["InventoryReport"];
export type CustomersReport = ApiSchemas["CustomersReport"];

export type ReportQuery = {
  from: string | null;
  to: string | null;
  groupBy: ReportGrouping | null;
};
