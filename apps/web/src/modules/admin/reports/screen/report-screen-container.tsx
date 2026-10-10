import type { SearchParams } from "nuqs/server";
import { Panel } from "@/components/panel";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { REPORTS, type ReportSlug } from "@/modules/admin/reports/definitions";
import { formatRange } from "@/modules/admin/reports/format";
import { tablesOf } from "@/modules/admin/reports/document";
import { loadReport, reportDocument } from "@/modules/admin/reports/registry";
import { ReportBlocks } from "@/modules/admin/reports/screen/report-blocks";
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
  const blocks = result.ok ? reportDocument(result.value) : [];

  return (
    <div className="flex flex-col gap-5">
      <AdminPageHeader
        title="Reportes"
        description="Importes en USD y fechas en hora de La Habana. Todo lo que ves aquí se exporta igual a PDF."
      />

      <ReportTabs active={slug} query={query} />

      <TableFrame>
        <ReportToolbar
          slug={slug}
          period={meta ? meta.period : null}
          groupBy={meta ? meta.groupBy : null}
          availableGroupings={meta ? meta.availableGroupings : []}
          hasSeries={definition.hasSeries}
          tables={tablesOf(blocks).map(({ id, title }) => ({ id, title }))}
        />

        <div className="flex flex-col gap-1">
          <h2 className="font-display text-xl font-semibold text-ink">{definition.title}</h2>
          <p className="text-sm text-ink-muted">
            {definition.summary}
            {meta && (
              <>
                {" "}
                Del {formatRange(meta.period.from, meta.period.to)}, comparado con el{" "}
                {formatRange(meta.previousPeriod.from, meta.previousPeriod.to)}.
              </>
            )}
          </p>
        </div>

        {result.ok ? <ReportBlocks blocks={blocks} /> : <Panel>{adminErrorMessage(result.error)}</Panel>}
      </TableFrame>
    </div>
  );
}
