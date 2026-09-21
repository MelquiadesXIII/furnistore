import Link from "next/link";
import { redirect } from "next/navigation";
import { Panel } from "@/components/panel";
import { toUserMessage } from "@/lib/errors";
import { getCart } from "@/modules/cart/api";
import { CartGrill } from "@/modules/cart/list/cart-grill";

export async function CartContainer() {
  const result = await getCart();

  if (!result.ok && result.error.kind === "unauthorized") {
    redirect("/login?next=/cart");
  }

  return (
    <div className="mx-auto w-full max-w-5xl flex-1 px-6 py-10">
      <h1 className="mb-8 border-b border-hairline pb-6 font-display text-3xl font-semibold tracking-tight text-ink">
        Carrito
      </h1>

      {!result.ok ? (
        <Panel>{toUserMessage(result.error)}</Panel>
      ) : result.value.items.length === 0 ? (
        <Panel>
          Tu carrito está vacío.{" "}
          <Link href="/" className="font-medium text-accent hover:underline">
            Explora el catálogo
          </Link>
        </Panel>
      ) : (
        <CartGrill cart={result.value} />
      )}
    </div>
  );
}
