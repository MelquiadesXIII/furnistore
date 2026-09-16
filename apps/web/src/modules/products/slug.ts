import type { Product } from "@/modules/products/types";

const DIACRITICS_PATTERN = /[̀-ͯ]/g;
const NON_ALPHANUMERIC_PATTERN = /[^a-z0-9]+/g;
const EDGE_DASH_PATTERN = /^-+|-+$/g;
const TRAILING_ID_PATTERN = /-(\d+)$/;

export function slugify(name: string): string {
  const slug = name
    .normalize("NFD")
    .replace(DIACRITICS_PATTERN, "")
    .toLowerCase()
    .replace(NON_ALPHANUMERIC_PATTERN, "-")
    .replace(EDGE_DASH_PATTERN, "");

  return slug || "producto";
}

export function buildProductHref(product: Pick<Product, "id" | "name">): string {
  return `/${slugify(product.name)}-${product.id}`;
}

export function parseProductId(productSlug: string): number | null {
  const match = TRAILING_ID_PATTERN.exec(productSlug);
  if (!match) return null;

  const id = Number.parseInt(match[1], 10);
  return Number.isFinite(id) && id > 0 ? id : null;
}
