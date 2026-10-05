import { authedApiFetch } from "@/lib/api/authed";
import type { ApiSchemas } from "@/lib/api/contract";
import { toQueryString } from "@/lib/api/query";
import type { Result } from "@/lib/result";
import type {
  AdminOrder,
  AdminOrderPage,
  AdminOrderStats,
} from "@/modules/admin/orders/types";
import type { OrderStatus } from "@/modules/orders/types";

export const ADMIN_ORDERS_PAGE_SIZE = 20;

export function getAdminOrders(query: {
  page: number;
  q: string;
  status: OrderStatus | null;
  sort: string;
  customerId?: number;
  pageSize?: number;
}): Promise<Result<AdminOrderPage>> {
  return authedApiFetch<AdminOrderPage>(
    `/api/admin/orders${toQueryString({
      page: query.page,
      pageSize: query.pageSize ?? ADMIN_ORDERS_PAGE_SIZE,
      search: query.q,
      status: query.status,
      sort: query.sort,
      customerId: query.customerId,
    })}`,
  );
}

export function getAdminOrderStats(): Promise<Result<AdminOrderStats>> {
  return authedApiFetch<AdminOrderStats>("/api/admin/orders/stats");
}

export function getAdminOrder(id: number): Promise<Result<AdminOrder>> {
  return authedApiFetch<AdminOrder>(`/api/admin/orders/${id}`);
}

export function transitionAdminOrder(
  id: number,
  action: "prepare" | "ship" | "deliver",
): Promise<Result<AdminOrder>> {
  return authedApiFetch<AdminOrder>(`/api/admin/orders/${id}/${action}`, { method: "POST" });
}

export function cancelAdminOrder(id: number, reason: string | null): Promise<Result<AdminOrder>> {
  return authedApiFetch<AdminOrder>(`/api/admin/orders/${id}/cancel`, {
    method: "POST",
    body: { reason } satisfies ApiSchemas["AdminCancelOrderRequest"],
  });
}
