import type { ApiSchemas } from "@/lib/api/contract";

export type AuthTokens = ApiSchemas["AuthTokensResponse"];

export type RegisterResult = ApiSchemas["RegisterResponse"];

export type AuthFailure = {
  code: string | null;
  message: string;
};
