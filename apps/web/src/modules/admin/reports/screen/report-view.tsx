import type { LoadedReport } from "@/modules/admin/reports/registry";
import { CancellationsReportView } from "@/modules/admin/reports/views/cancellations/cancellations-report";
import { CustomersReportView } from "@/modules/admin/reports/views/customers/customers-report";
import { InventoryReportView } from "@/modules/admin/reports/views/inventory/inventory-report";
import { OperationsReportView } from "@/modules/admin/reports/views/operations/operations-report";
import { ProductsReportView } from "@/modules/admin/reports/views/products/products-report";
import { SalesReportView } from "@/modules/admin/reports/views/sales/sales-report";

export function ReportView({ report }: { report: LoadedReport }) {
  switch (report.slug) {
    case "sales":
      return <SalesReportView report={report.data} />;
    case "products":
      return <ProductsReportView report={report.data} />;
    case "operations":
      return <OperationsReportView report={report.data} />;
    case "cancellations":
      return <CancellationsReportView report={report.data} />;
    case "inventory":
      return <InventoryReportView report={report.data} />;
    case "customers":
      return <CustomersReportView report={report.data} />;
  }
}
