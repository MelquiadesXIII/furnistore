import type { SearchParams } from "nuqs/server";
import { Panel } from "@/components/panel";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { REPORTS, type ReportSlug } from "@/modules/admin/reports/definitions";
import { formatRange } from "@/modules/admin/reports/format";
import { loadReport, reportTable } from "@/modules/admin/reports/registry";
import { ReportView } from "@/modules/admin/reports/screen/report-view";
import { loadReportSearchParams } from "@/modules/admin/reports/search-params";
import { ReportTabs } from "@/modules/admin/reports/shared/report-tabs";
import { ReportToolbar } from "@/modules/admin/reports/shared/report-toolbar";
import { AdminPageHeader } from "@/modules/admin/shell/admin-page-header";
import { TableFrame } from "@/modules/admin/table/table-frame";

export async function ReportScreenContainer({
  slug,
  searchParams,
}: {
  slug: ReportSlug;
  searchParams: Promise<SearchParams>;
}) {
  const query = await loadReportSearchParams(searchParams);
  const result = await loadReport(slug, query);
  const definition = REPORTS[slug];
  const meta = result.ok ? result.value.data.meta : null;
  const table = result.ok ? reportTable(result.value) : null;

  return (
    <div className="flex flex-col gap-5">
      <AdminPageHeader
        title="Reportes"
        description="Reportes de la tienda. Cada uno se puede descargar en PDF o su tabla en CSV."
      />

      <ReportTabs active={slug} query={query} />

      <TableFrame>
        <ReportToolbar
          slug={slug}
          period={meta ? meta.period : null}
          groupBy={meta ? meta.groupBy : null}
          availableGroupings={meta ? meta.availableGroupings : []}
          table={table ? { id: table.id, title: table.title } : null}
        />

        <div className="flex flex-col gap-1">
          <h2 className="font-display text-xl font-semibold text-ink">{definition.title}</h2>
          <p className="text-sm text-ink-muted">
            {definition.summary}
            {meta && definition.usesPeriod && <> Período: {formatRange(meta.period.from, meta.period.to)}.</>}
          </p>
        </div>

        {result.ok ? <ReportView report={result.value} /> : <Panel>{adminErrorMessage(result.error)}</Panel>}
      </TableFrame>
    </div>
  );
}
