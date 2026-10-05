import { createLoader, parseAsInteger, parseAsString, parseAsStringLiteral } from "nuqs/server";

export const productSearchParams = {
  page: parseAsInteger.withDefault(1),
  q: parseAsString.withDefault(""),
  status: parseAsStringLiteral(["Active", "Archived"] as const),
  categoryId: parseAsInteger,
  maxStock: parseAsInteger,
  sort: parseAsString.withDefault("name"),
};

export const loadProductSearchParams = createLoader(productSearchParams);
