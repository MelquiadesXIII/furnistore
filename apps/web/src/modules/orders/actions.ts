"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { cancelOrder as cancelOrderRequest, checkout } from "@/modules/orders/api";
import { orderErrorMessage } from "@/modules/orders/error-messages";

export type OrderActionResult = { ok: true } | { ok: false; message: string };

const MAX_TOTAL = 100_000_000;
const MAX_REASON_LENGTH = 300;

export async function placeOrder(expectedTotal: number): Promise<OrderActionResult> {
  if (
    typeof expectedTotal !== "number" ||
    !Number.isFinite(expectedTotal) ||
    expectedTotal < 0 ||
    expectedTotal > MAX_TOTAL
  ) {
    return { ok: false, message: "El total no es válido. Recarga la página." };
  }

  const result = await checkout(expectedTotal);

  if (result.ok) {
    revalidatePath("/", "layout");
    redirect(`/orders/${result.value.id}?placed=1`);
  }

  const { error } = result;

  if (error.kind === "unauthorized") {
    redirect("/login?next=/checkout");
  }

  if (error.kind === "timeout" || error.kind === "network") {
    return {
      ok: false,
      message:
        "No pudimos confirmar si tu pedido se registró. Revisa «Mis pedidos» antes de intentarlo de nuevo.",
    };
  }

  revalidatePath("/", "layout");

  if (error.code === "checkout.empty_cart") {
    redirect("/cart");
  }

  return { ok: false, message: orderErrorMessage(error) };
}

export async function cancelOrder(orderId: number, reason: unknown): Promise<OrderActionResult> {
  if (typeof orderId !== "number" || !Number.isInteger(orderId) || orderId <= 0) {
    return { ok: false, message: "Pedido no válido." };
  }

  const trimmed = typeof reason === "string" ? reason.trim() : "";
  if (trimmed.length > MAX_REASON_LENGTH) {
    return { ok: false, message: `El motivo admite como máximo ${MAX_REASON_LENGTH} caracteres.` };
  }

  const result = await cancelOrderRequest(orderId, trimmed || null);

  if (!result.ok && result.error.kind === "unauthorized") {
    redirect(`/login?next=/orders/${orderId}`);
  }

  revalidatePath("/", "layout");

  return result.ok ? { ok: true } : { ok: false, message: orderErrorMessage(result.error) };
}
