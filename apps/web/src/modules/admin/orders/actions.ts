"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import type { AdminActionResult } from "@/modules/admin/action-result";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { cancelAdminOrder, transitionAdminOrder } from "@/modules/admin/orders/api";


const TRANSITIONS = ["prepare", "ship", "deliver"] as const;
const MAX_REASON_LENGTH = 300;

function isOrderId(value: unknown): value is number {
  return typeof value === "number" && Number.isInteger(value) && value > 0;
}

export async function advanceOrder(orderId: unknown, action: unknown): Promise<AdminActionResult> {
  if (!isOrderId(orderId) || !TRANSITIONS.includes(action as (typeof TRANSITIONS)[number])) {
    return { ok: false, message: "Acción no válida." };
  }

  const result = await transitionAdminOrder(orderId, action as (typeof TRANSITIONS)[number]);
  return settle(result.ok ? null : result.error, orderId);
}

export async function cancelOrderAsAdmin(orderId: unknown, reason: unknown): Promise<AdminActionResult> {
  if (!isOrderId(orderId)) return { ok: false, message: "Pedido no válido." };

  const trimmed = typeof reason === "string" ? reason.trim() : "";
  if (trimmed.length > MAX_REASON_LENGTH) {
    return { ok: false, message: `El motivo admite como máximo ${MAX_REASON_LENGTH} caracteres.` };
  }

  const result = await cancelAdminOrder(orderId, trimmed || null);
  return settle(result.ok ? null : result.error, orderId);
}

function settle(
  error: Parameters<typeof adminErrorMessage>[0] | null,
  orderId: number,
): AdminActionResult {
  if (error?.kind === "unauthorized") {
    redirect(`/login?next=/admin/orders/${orderId}`);
  }

  revalidatePath("/", "layout");
  return error ? { ok: false, message: adminErrorMessage(error) } : { ok: true };
}
