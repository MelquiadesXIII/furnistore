import { cache } from "react";
import { authedApiFetch } from "@/lib/api/authed";
import type { ApiSchemas } from "@/lib/api/contract";
import { toQueryString } from "@/lib/api/query";
import type { Result } from "@/lib/result";
import type { AdminCategory, AdminCategoryPage } from "@/modules/admin/categories/types";

export const getAdminCategories = cache(
  (q = "", sort = "name"): Promise<Result<AdminCategoryPage>> =>
    authedApiFetch<AdminCategoryPage>(
      `/api/admin/product-categories${toQueryString({ page: 1, pageSize: 100, search: q, sort })}`,
    ),
);

export function createAdminCategory(name: string): Promise<Result<AdminCategory>> {
  return authedApiFetch<AdminCategory>("/api/admin/product-categories", {
    method: "POST",
    body: { name } satisfies ApiSchemas["SaveCategoryRequest"],
  });
}

export function renameAdminCategory(id: number, name: string): Promise<Result<AdminCategory>> {
  return authedApiFetch<AdminCategory>(`/api/admin/product-categories/${id}`, {
    method: "PUT",
    body: { name } satisfies ApiSchemas["SaveCategoryRequest"],
  });
}

export function deleteAdminCategory(id: number): Promise<Result<void>> {
  return authedApiFetch<void>(`/api/admin/product-categories/${id}`, { method: "DELETE" });
}
