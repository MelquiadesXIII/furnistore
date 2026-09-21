import { ShoppingBag } from "lucide-react";
import Link from "next/link";
import { getCart } from "@/modules/cart/api";

export function CartLink({ count }: { count: number | null }) {
  const label =
    count === null ? "Carrito" : `Carrito: ${count} ${count === 1 ? "artículo" : "artículos"}`;

  return (
    <Link
      href="/cart"
      aria-label={label}
      className="relative flex h-8 w-8 items-center justify-center rounded-full text-ink transition-colors hover:text-accent focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
    >
      <ShoppingBag className="h-5 w-5" />
      {count !== null && count > 0 && (
        <span
          aria-hidden="true"
          className="absolute -top-1 -right-1 flex h-4 min-w-4 items-center justify-center rounded-full bg-accent px-1 font-mono text-[10px] leading-none text-accent-ink"
        >
          {count > 99 ? "99+" : count}
        </span>
      )}
    </Link>
  );
}

export async function CartBadge() {
  const result = await getCart();
  const count = result.ok ? result.value.items.reduce((total, item) => total + item.quantity, 0) : null;

  return <CartLink count={count} />;
}
