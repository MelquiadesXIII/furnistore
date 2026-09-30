import Link from "next/link";
import { redirect } from "next/navigation";
import { Panel } from "@/components/panel";
import { getAccount } from "@/modules/account/api";
import { accountErrorMessage } from "@/modules/account/error-messages";
import { getCart } from "@/modules/cart/api";
import { isPurchasable } from "@/modules/cart/cart-rules";
import { cartErrorMessage } from "@/modules/cart/error-messages";
import { CheckoutShipping } from "@/modules/orders/checkout/checkout-shipping";
import { CheckoutSummary } from "@/modules/orders/checkout/checkout-summary";

export async function CheckoutContainer() {
  const [cart, account] = await Promise.all([getCart(), getAccount()]);

  if (
    (!cart.ok && cart.error.kind === "unauthorized") ||
    (!account.ok && account.error.kind === "unauthorized")
  ) {
    redirect("/login?next=/checkout");
  }

  if (cart.ok && cart.value.items.length === 0) {
    redirect("/cart");
  }

  return (
    <div className="mx-auto w-full max-w-5xl flex-1 px-6 py-10">
      <div className="mb-8 flex flex-col gap-2 border-b border-hairline pb-6">
        <Link href="/cart" className="text-sm text-ink-muted transition-colors hover:text-accent">
          ← Volver al carrito
        </Link>
        <h1 className="font-display text-3xl font-semibold tracking-tight text-ink">
          Finalizar compra
        </h1>
      </div>

      {!cart.ok ? (
        <Panel>{cartErrorMessage(cart.error)}</Panel>
      ) : !account.ok ? (
        <Panel>{accountErrorMessage(account.error)}</Panel>
      ) : !cart.value.items.every(isPurchasable) ? (
        <Panel>
          Algunos productos de tu carrito ya no están disponibles en esa cantidad.{" "}
          <Link href="/cart" className="font-medium text-accent hover:underline">
            Revisa tu carrito
          </Link>
        </Panel>
      ) : (
        <div className="grid items-start gap-8 lg:grid-cols-[minmax(0,1fr)_22rem]">
          <CheckoutShipping account={account.value} />
          <CheckoutSummary cart={cart.value} canPlaceOrder={account.value.isProfileComplete} />
        </div>
      )}
    </div>
  );
}
