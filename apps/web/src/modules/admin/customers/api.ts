import { authedApiFetch } from "@/lib/api/authed";
import type { ApiSchemas } from "@/lib/api/contract";
import { toQueryString } from "@/lib/api/query";
import type { Result } from "@/lib/result";
import type { AdminCustomer, AdminCustomerPage, CustomerFilter } from "@/modules/admin/customers/types";

export const ADMIN_CUSTOMERS_PAGE_SIZE = 20;

export type AccountAction = "unlock" | "disable" | "enable" | "grant-admin" | "revoke-admin";

export function getAdminCustomers(query: {
  page: number;
  q: string;
  filter: CustomerFilter | null;
  sort: string;
}): Promise<Result<AdminCustomerPage>> {
  return authedApiFetch<AdminCustomerPage>(
    `/api/admin/customers${toQueryString({
      page: query.page,
      pageSize: ADMIN_CUSTOMERS_PAGE_SIZE,
      search: query.q,
      filter: query.filter,
      sort: query.sort,
    })}`,
  );
}

export function getAdminCustomer(id: number): Promise<Result<AdminCustomer>> {
  return authedApiFetch<AdminCustomer>(`/api/admin/customers/${id}`);
}

export function updateAdminCustomer(
  id: number,
  request: ApiSchemas["UpdateCustomerRequest"],
): Promise<Result<AdminCustomer>> {
  return authedApiFetch<AdminCustomer>(`/api/admin/customers/${id}`, { method: "PUT", body: request });
}

const ACCOUNT_ROUTES: Record<AccountAction, { path: string; method: "POST" | "DELETE" }> = {
  unlock: { path: "unlock", method: "POST" },
  disable: { path: "disable", method: "POST" },
  enable: { path: "enable", method: "POST" },
  "grant-admin": { path: "admin-role", method: "POST" },
  "revoke-admin": { path: "admin-role", method: "DELETE" },
};

export function changeCustomerAccount(id: number, action: AccountAction): Promise<Result<AdminCustomer>> {
  const route = ACCOUNT_ROUTES[action];
  return authedApiFetch<AdminCustomer>(`/api/admin/customers/${id}/${route.path}`, { method: route.method });
}
