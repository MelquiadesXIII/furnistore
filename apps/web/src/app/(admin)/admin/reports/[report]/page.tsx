import type { Metadata } from "next";
import { notFound } from "next/navigation";
import type { SearchParams } from "nuqs/server";
import { isReportSlug, REPORTS } from "@/modules/admin/reports/definitions";
import { ReportScreenContainer } from "@/modules/admin/reports/screen/report-screen-container";

type Props = { params: Promise<{ report: string }>; searchParams: Promise<SearchParams> };

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { report } = await params;
  return { title: isReportSlug(report) ? `Reporte de ${REPORTS[report].title.toLowerCase()}` : "Reportes" };
}

export default async function Page({ params, searchParams }: Props) {
  const { report } = await params;
  if (!isReportSlug(report)) notFound();

  return <ReportScreenContainer slug={report} searchParams={searchParams} />;
}
