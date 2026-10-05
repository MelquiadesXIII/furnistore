import type { Metadata } from "next";
import type { SearchParams } from "nuqs/server";
import { AdminProductsContainer } from "@/modules/admin/products/list/admin-products-container";

export const metadata: Metadata = { title: "Productos" };

export default function Page({ searchParams }: { searchParams: Promise<SearchParams> }) {
  return <AdminProductsContainer searchParams={searchParams} />;
}
