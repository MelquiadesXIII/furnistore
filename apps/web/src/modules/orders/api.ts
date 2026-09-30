import { cache } from "react";
import { authedApiFetch } from "@/lib/api/authed";
import type { ApiSchemas } from "@/lib/api/contract";
import type { Result } from "@/lib/result";
import type { Order, OrderPage } from "@/modules/orders/types";

const CHECKOUT_TIMEOUT_MS = 20_000;

export function getOrders({
  page,
  pageSize,
}: {
  page: number;
  pageSize: number;
}): Promise<Result<OrderPage>> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
  return authedApiFetch<OrderPage>(`/api/orders?${params}`);
}

export const getOrder = cache(
  (id: number): Promise<Result<Order>> => authedApiFetch<Order>(`/api/orders/${id}`),
);

export function checkout(expectedTotal: number): Promise<Result<Order>> {
  return authedApiFetch<Order>("/api/orders/checkout", {
    method: "POST",
    body: { expectedTotal } satisfies ApiSchemas["CheckoutRequest"],
    timeoutMs: CHECKOUT_TIMEOUT_MS,
  });
}

export function cancelOrder(id: number, reason: string | null): Promise<Result<Order>> {
  return authedApiFetch<Order>(`/api/orders/${id}/cancel`, {
    method: "POST",
    body: { reason } satisfies ApiSchemas["CancelOrderRequest"],
  });
}
