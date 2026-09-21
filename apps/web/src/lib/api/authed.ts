import "server-only";

import { apiFetch, type ApiFetchInit } from "@/lib/api/client";
import type { AppError } from "@/lib/errors";
import { err, type Result } from "@/lib/result";
import { getSession } from "@/lib/session";

export async function authedApiFetch<T>(
  path: string,
  init: ApiFetchInit = {},
): Promise<Result<T, AppError>> {
  const token = await getSession();
  if (!token) return err({ kind: "unauthorized", messages: [] });

  return apiFetch<T>(path, {
    ...init,
    headers: { ...init.headers, Authorization: `Bearer ${token}` },
  });
}
