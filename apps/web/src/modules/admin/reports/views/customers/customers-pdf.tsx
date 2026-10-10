import { formatValue } from "@/modules/admin/reports/format";
import { PdfBarsChart } from "@/modules/admin/reports/pdf/pdf-charts";
import { CHART_WIDTH, PdfChartBox, PdfStats, PdfTable } from "@/modules/admin/reports/pdf/pdf-layout";
import type { CustomersReport } from "@/modules/admin/reports/types";
import { customersTable } from "@/modules/admin/reports/views/customers/customers-table";

export function CustomersPdf({ report }: { report: CustomersReport }) {
  const provinces = report.provinces.slice(0, 10).map((row) => ({ label: row.name, value: row.revenue }));

  return (
    <>
      <PdfStats
        stats={[
          { label: "Compradores", value: formatValue("count", report.buyers) },
          { label: "Nuevos", value: formatValue("count", report.newBuyers) },
          { label: "Recurrentes", value: formatValue("count", report.returningBuyers) },
        ]}
      />
      <PdfChartBox
        title="Ingresos por provincia"
        empty={provinces.length === 0 ? "No hubo ventas en este período." : undefined}
      >
        <PdfBarsChart items={provinces} kind="money" width={CHART_WIDTH} horizontal />
      </PdfChartBox>
      <PdfTable table={customersTable(report)} />
    </>
  );
}
