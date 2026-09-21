"use client";

import { useOptimistic, useState, useTransition } from "react";
import {
  removeFromCart,
  setCartItemQuantity,
  type CartActionResult,
} from "@/modules/cart/actions";
import { CartItemCard } from "@/modules/cart/list/cart-item-card";
import { CartSummary } from "@/modules/cart/list/cart-summary";
import type { Cart, CartItem } from "@/modules/cart/types";

type CartChange =
  | { type: "quantity"; productId: number; quantity: number }
  | { type: "remove"; productId: number };

function applyChange(items: CartItem[], change: CartChange): CartItem[] {
  if (change.type === "remove") {
    return items.filter((item) => item.productId !== change.productId);
  }

  return items.map((item) =>
    item.productId === change.productId ? { ...item, quantity: change.quantity } : item,
  );
}

export function CartGrill({ cart }: { cart: Cart }) {
  const [items, applyOptimistic] = useOptimistic(cart.items, applyChange);
  const [pending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);

  function run(change: CartChange, action: () => Promise<CartActionResult>) {
    setError(null);
    startTransition(async () => {
      applyOptimistic(change);
      const result = await action();
      if (!result.ok) setError(result.message);
    });
  }

  const itemCount = items.reduce((total, item) => total + item.quantity, 0);
  const settledQuantities = new Map(cart.items.map((item) => [item.productId, item.quantity]));

  return (
    <div className="grid items-start gap-8 lg:grid-cols-[minmax(0,1fr)_20rem]">
      <div className="flex flex-col gap-4">
        {error && (
          <p role="alert" className="text-sm text-brick">
            {error}
          </p>
        )}
        <ul className="flex flex-col gap-4">
          {items.map((item) => (
            <CartItemCard
              key={item.productId}
              item={item}
              lineTotal={
                settledQuantities.get(item.productId) === item.quantity ? item.lineTotal : null
              }
              disabled={pending}
              onQuantityChange={(quantity) =>
                run({ type: "quantity", productId: item.productId, quantity }, () =>
                  setCartItemQuantity(item.productId, quantity),
                )
              }
              onRemove={() =>
                run({ type: "remove", productId: item.productId }, () =>
                  removeFromCart(item.productId),
                )
              }
            />
          ))}
        </ul>
      </div>

      <CartSummary
        subtotal={cart.subtotal}
        shippingCost={cart.shippingCost}
        total={cart.total}
        itemCount={itemCount}
        pending={pending}
      />
    </div>
  );
}
