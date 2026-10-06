import { createLoader, parseAsInteger, parseAsString, parseAsStringLiteral } from "nuqs/server";

export const customerSearchParams = {
  page: parseAsInteger.withDefault(1),
  q: parseAsString.withDefault(""),
  filter: parseAsStringLiteral(["Admins", "Disabled", "LockedOut", "Unconfirmed"] as const),
  sort: parseAsString.withDefault("name"),
};

export const loadCustomerSearchParams = createLoader(customerSearchParams);
