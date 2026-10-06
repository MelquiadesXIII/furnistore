import type { Metadata } from "next";
import type { SearchParams } from "nuqs/server";
import { AdminCustomersContainer } from "@/modules/admin/customers/list/admin-customers-container";

export const metadata: Metadata = { title: "Clientes" };

export default function Page({ searchParams }: { searchParams: Promise<SearchParams> }) {
  return <AdminCustomersContainer searchParams={searchParams} />;
}
