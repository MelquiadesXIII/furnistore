import { apiFetch } from "@/lib/api/client";
import type { Result } from "@/lib/result";
import type { Product, ProductPage } from "@/modules/products/types";

export type ProductSearchParams = {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: number;
};

export function getProducts(
  params: ProductSearchParams = {},
): Promise<Result<ProductPage>> {
  const query = new URLSearchParams();

  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  if (params.search) query.set("search", params.search);
  if (params.categoryId) query.set("categoryId", String(params.categoryId));

  const qs = query.toString();
  return apiFetch<ProductPage>(`/api/products${qs ? `?${qs}` : ""}`);
}

export function getProduct(id: number): Promise<Result<Product>> {
  return apiFetch<Product>(`/api/products/${id}`);
}
