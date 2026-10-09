import { cn } from "cn";
import Link from "next/link";
import { REPORT_SLUGS, REPORTS, type ReportSlug } from "@/modules/admin/reports/definitions";
import { serializeReportQuery } from "@/modules/admin/reports/search-params";
import type { ReportQuery } from "@/modules/admin/reports/types";

export function ReportTabs({ active, query }: { active: ReportSlug; query: ReportQuery }) {
  return (
    <nav aria-label="Reportes" className="flex gap-1 overflow-x-auto border-b border-hairline">
      {REPORT_SLUGS.map((slug) => {
        const current = slug === active;
        return (
          <Link
            key={slug}
            href={serializeReportQuery(`/admin/reports/${slug}`, query)}
            aria-current={current ? "page" : undefined}
            className={cn(
              "-mb-px shrink-0 border-b-2 border-transparent px-3 py-2 text-sm text-ink-muted transition-colors hover:text-ink",
              current && "border-accent font-medium text-ink",
            )}
          >
            {REPORTS[slug].title}
          </Link>
        );
      })}
    </nav>
  );
}
