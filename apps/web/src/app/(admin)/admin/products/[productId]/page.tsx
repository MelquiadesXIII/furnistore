import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { EditProductContainer } from "@/modules/admin/products/form/edit-product-container";
import { parseRouteId } from "@/modules/admin/route-id";

export const metadata: Metadata = { title: "Producto" };

export default async function Page({ params }: { params: Promise<{ productId: string }> }) {
  const id = parseRouteId((await params).productId);
  if (id === null) notFound();

  return <EditProductContainer id={id} />;
}
