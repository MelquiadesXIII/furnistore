import type { CartItem } from "@/modules/cart/types";

export function isPurchasable(item: CartItem): boolean {
  return item.isActive && item.quantity <= item.stock;
}
