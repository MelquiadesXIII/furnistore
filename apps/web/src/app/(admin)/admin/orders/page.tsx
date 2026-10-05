import type { Metadata } from "next";
import type { SearchParams } from "nuqs/server";
import { AdminOrdersContainer } from "@/modules/admin/orders/list/admin-orders-container";

export const metadata: Metadata = { title: "Pedidos" };

export default function Page({ searchParams }: { searchParams: Promise<SearchParams> }) {
  return <AdminOrdersContainer searchParams={searchParams} />;
}
