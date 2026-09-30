import { cache } from "react";
import { authedApiFetch } from "@/lib/api/authed";
import type { ApiSchemas } from "@/lib/api/contract";
import type { Result } from "@/lib/result";
import type { Account } from "@/modules/account/types";

export const getAccount = cache(
  (): Promise<Result<Account>> => authedApiFetch<Account>("/api/clients/me"),
);

export function updateAccount(request: ApiSchemas["UpdateClientRequest"]): Promise<Result<void>> {
  return authedApiFetch<void>("/api/clients/me", { method: "PUT", body: request });
}
