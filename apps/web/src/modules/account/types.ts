import type { ApiSchemas } from "@/lib/api/contract";

export type Account = ApiSchemas["ClientResponse"];

export type ShippingAddress = ApiSchemas["ShippingAddress"];

export type AccountField =
  | "firstName"
  | "lastName"
  | "phone"
  | "street"
  | "city"
  | "province"
  | "deliveryNotes";

export type AccountFieldErrors = Partial<Record<AccountField, string>>;
