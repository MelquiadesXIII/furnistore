import type { ApiSchemas } from "@/lib/api/contract";

export type Order = ApiSchemas["OrderResponse"];

export type OrderLine = ApiSchemas["OrderLineResponse"];

export type OrderStatus = ApiSchemas["OrderStatus"];

export type OrderPage = ApiSchemas["OrderResponsePagedResult"];
