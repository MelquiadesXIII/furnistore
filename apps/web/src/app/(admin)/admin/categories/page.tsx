import type { Metadata } from "next";
import type { SearchParams } from "nuqs/server";
import { CategoriesContainer } from "@/modules/admin/categories/categories-container";

export const metadata: Metadata = { title: "Categorías" };

export default function Page({ searchParams }: { searchParams: Promise<SearchParams> }) {
  return <CategoriesContainer searchParams={searchParams} />;
}
