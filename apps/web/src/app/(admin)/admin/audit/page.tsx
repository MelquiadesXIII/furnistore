import type { Metadata } from "next";
import type { SearchParams } from "nuqs/server";
import { AuditContainer } from "@/modules/admin/audit/audit-container";

export const metadata: Metadata = { title: "Auditoría" };

export default function Page({ searchParams }: { searchParams: Promise<SearchParams> }) {
  return <AuditContainer searchParams={searchParams} />;
}
