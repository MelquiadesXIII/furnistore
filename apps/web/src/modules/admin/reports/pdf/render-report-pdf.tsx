import "server-only";

import { renderToBuffer } from "@react-pdf/renderer";
import { REPORTS } from "@/modules/admin/reports/definitions";
import { PdfReport } from "@/modules/admin/reports/pdf/pdf-layout";
import type { LoadedReport } from "@/modules/admin/reports/registry";
import { CancellationsPdf } from "@/modules/admin/reports/views/cancellations/cancellations-pdf";
import { CustomersPdf } from "@/modules/admin/reports/views/customers/customers-pdf";
import { InventoryPdf } from "@/modules/admin/reports/views/inventory/inventory-pdf";
import { OperationsPdf } from "@/modules/admin/reports/views/operations/operations-pdf";
import { ProductsPdf } from "@/modules/admin/reports/views/products/products-pdf";
import { SalesPdf } from "@/modules/admin/reports/views/sales/sales-pdf";

function ReportContent({ report }: { report: LoadedReport }) {
  switch (report.slug) {
    case "sales":
      return <SalesPdf report={report.data} />;
    case "products":
      return <ProductsPdf report={report.data} />;
    case "operations":
      return <OperationsPdf report={report.data} />;
    case "cancellations":
      return <CancellationsPdf report={report.data} />;
    case "inventory":
      return <InventoryPdf report={report.data} />;
    case "customers":
      return <CustomersPdf report={report.data} />;
  }
}

export function renderReportPdf(report: LoadedReport, generatedBy: string): Promise<Buffer> {
  const definition = REPORTS[report.slug];

  return renderToBuffer(
    <PdfReport
      title={definition.title}
      meta={report.data.meta}
      generatedBy={generatedBy}
      usesPeriod={definition.usesPeriod}
    >
      <ReportContent report={report} />
    </PdfReport>,
  );
}
