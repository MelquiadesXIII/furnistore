import { cache } from "react";
import { authedApiFetch } from "@/lib/api/authed";
import type { ApiSchemas } from "@/lib/api/contract";
import type { Result } from "@/lib/result";
import type { Cart } from "@/modules/cart/types";

export const getCart = cache((): Promise<Result<Cart>> => authedApiFetch<Cart>("/api/cart"));

export function addCartItem(productId: number, quantity: number): Promise<Result<Cart>> {
  return authedApiFetch<Cart>("/api/cart/items", {
    method: "POST",
    body: { productId, quantity } satisfies ApiSchemas["AddCartItemRequest"],
  });
}

export function updateCartItem(productId: number, quantity: number): Promise<Result<Cart>> {
  return authedApiFetch<Cart>(`/api/cart/items/${productId}`, {
    method: "PUT",
    body: { quantity } satisfies ApiSchemas["UpdateCartItemRequest"],
  });
}

export function removeCartItem(productId: number): Promise<Result<Cart>> {
  return authedApiFetch<Cart>(`/api/cart/items/${productId}`, { method: "DELETE" });
}
