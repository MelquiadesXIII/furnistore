"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import type { Result } from "@/lib/result";
import { safeNextPath } from "@/modules/auth/safe-next-path";
import { addCartItem, removeCartItem, updateCartItem } from "@/modules/cart/api";
import { cartErrorMessage } from "@/modules/cart/error-messages";
import type { Cart } from "@/modules/cart/types";

export type CartActionResult = { ok: true } | { ok: false; message: string };

const MAX_QUANTITY = 10_000;

export async function addToCart(
  productId: number,
  quantity: number,
  returnTo: string,
): Promise<CartActionResult> {
  if (!isPositiveInteger(productId) || !isPositiveInteger(quantity) || quantity > MAX_QUANTITY) {
    return { ok: false, message: "Cantidad no válida." };
  }

  return settle(await addCartItem(productId, quantity), safeNextPath(returnTo));
}

export async function setCartItemQuantity(
  productId: number,
  quantity: number,
): Promise<CartActionResult> {
  if (!isPositiveInteger(productId) || !isPositiveInteger(quantity) || quantity > MAX_QUANTITY) {
    return { ok: false, message: "Cantidad no válida." };
  }

  return settle(await updateCartItem(productId, quantity), "/cart");
}

export async function removeFromCart(productId: number): Promise<CartActionResult> {
  if (!isPositiveInteger(productId)) {
    return { ok: false, message: "Producto no válido." };
  }

  return settle(await removeCartItem(productId), "/cart");
}

function isPositiveInteger(value: unknown): value is number {
  return typeof value === "number" && Number.isInteger(value) && value > 0;
}

function settle(result: Result<Cart>, returnTo: string): CartActionResult {
  if (result.ok) {
    revalidatePath("/", "layout");
    return { ok: true };
  }

  if (result.error.kind === "unauthorized") {
    redirect(`/login?next=${encodeURIComponent(returnTo)}`);
  }

  if (result.error.kind === "conflict" || result.error.kind === "notFound") {
    revalidatePath("/", "layout");
  }

  return { ok: false, message: cartErrorMessage(result.error) };
}
