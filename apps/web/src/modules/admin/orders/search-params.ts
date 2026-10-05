import { createLoader, parseAsInteger, parseAsString, parseAsStringLiteral } from "nuqs/server";

export const ORDER_STATUSES = ["Paid", "Processing", "Shipped", "Delivered", "Cancelled"] as const;

export const orderSearchParams = {
  page: parseAsInteger.withDefault(1),
  q: parseAsString.withDefault(""),
  status: parseAsStringLiteral(ORDER_STATUSES),
  sort: parseAsString.withDefault("-placedAt"),
};

export const loadOrderSearchParams = createLoader(orderSearchParams);
