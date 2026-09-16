import { notFound } from "next/navigation";
import { ProductDetailContainer } from "@/modules/products/detail/product-detail-container";
import { parseProductId } from "@/modules/products/slug";

export default async function Page({
  params,
}: {
  params: Promise<{ productSlug: string }>;
}) {
  const { productSlug } = await params;
  const id = parseProductId(productSlug);

  if (id === null) {
    notFound();
  }

  return <ProductDetailContainer id={id} />;
}
