import { authedApiFetch } from "@/lib/api/authed";
import type { ApiSchemas } from "@/lib/api/contract";
import { toQueryString } from "@/lib/api/query";
import type { Result } from "@/lib/result";
import type { AdminProduct, AdminProductPage, ProductListStatus } from "@/modules/admin/products/types";

export const ADMIN_PRODUCTS_PAGE_SIZE = 20;

export function getAdminProducts(query: {
  page: number;
  q: string;
  status: ProductListStatus | null;
  categoryId: number | null;
  maxStock: number | null;
  sort: string;
  pageSize?: number;
}): Promise<Result<AdminProductPage>> {
  return authedApiFetch<AdminProductPage>(
    `/api/admin/products${toQueryString({
      page: query.page,
      pageSize: query.pageSize ?? ADMIN_PRODUCTS_PAGE_SIZE,
      search: query.q,
      status: query.status,
      categoryId: query.categoryId,
      maxStock: query.maxStock,
      sort: query.sort,
    })}`,
  );
}

export function getAdminProduct(id: number): Promise<Result<AdminProduct>> {
  return authedApiFetch<AdminProduct>(`/api/admin/products/${id}`);
}

export function createAdminProduct(
  request: ApiSchemas["CreateProductRequest"],
): Promise<Result<AdminProduct>> {
  return authedApiFetch<AdminProduct>("/api/admin/products", { method: "POST", body: request });
}

export function updateAdminProduct(
  id: number,
  request: ApiSchemas["UpdateProductRequest"],
): Promise<Result<AdminProduct>> {
  return authedApiFetch<AdminProduct>(`/api/admin/products/${id}`, { method: "PUT", body: request });
}

export function deleteAdminProduct(id: number): Promise<Result<void>> {
  return authedApiFetch<void>(`/api/admin/products/${id}`, { method: "DELETE" });
}
