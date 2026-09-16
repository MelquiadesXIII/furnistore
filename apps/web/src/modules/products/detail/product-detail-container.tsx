import { notFound } from "next/navigation";
import { toUserMessage } from "@/lib/errors";
import { getSession } from "@/lib/session";
import { getProduct } from "@/modules/products/api";
import { ProductDetailView } from "@/modules/products/detail/product-detail-view";
import { Panel } from "@/modules/products/panel";

export async function ProductDetailContainer({ id }: { id: number }) {
  const isAuthenticated = Boolean(await getSession());
  const result = await getProduct(id);

  if (!result.ok && result.error.kind === "notFound") {
    notFound();
  }

  return (
    <div className="mx-auto w-full max-w-4xl flex-1 px-6 py-10">
      {!result.ok ? (
        <Panel>{toUserMessage(result.error)}</Panel>
      ) : (
        <ProductDetailView product={result.value} isAuthenticated={isAuthenticated} />
      )}
    </div>
  );
}
