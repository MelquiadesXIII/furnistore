import { notFound } from "next/navigation";
import { Panel } from "@/components/panel";
import { toUserMessage } from "@/lib/errors";
import { getSession } from "@/lib/session";
import { getCart } from "@/modules/cart/api";
import { getProduct } from "@/modules/products/api";
import { ProductDetailView } from "@/modules/products/detail/product-detail-view";

export async function ProductDetailContainer({ id }: { id: number }) {
  const isAuthenticated = Boolean(await getSession());
  const [result, cart] = await Promise.all([getProduct(id), isAuthenticated ? getCart() : null]);
  const inCart = cart?.ok ? (cart.value.items.find((item) => item.productId === id)?.quantity ?? 0) : 0;

  if (!result.ok && result.error.kind === "notFound") {
    notFound();
  }

  return (
    <div className="mx-auto w-full max-w-4xl flex-1 px-6 py-10">
      {!result.ok ? (
        <Panel>{toUserMessage(result.error)}</Panel>
      ) : (
        <ProductDetailView product={result.value} isAuthenticated={isAuthenticated} inCart={inCart} />
      )}
    </div>
  );
}
