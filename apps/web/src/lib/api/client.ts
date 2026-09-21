import "server-only";

import type { AppError, AppErrorKind } from "@/lib/errors";
import { err, ok, type Result } from "@/lib/result";

const DEFAULT_TIMEOUT_MS = 8000;

function getBaseUrl(): string {
  const baseUrl = process.env.API_BASE_URL;
  if (!baseUrl) {
    throw new Error("API_BASE_URL no está configurada (ver apps/web/.env.example)");
  }
  return baseUrl;
}

function kindForStatus(status: number): AppErrorKind {
  if (status === 401 || status === 403) return "unauthorized";
  if (status === 404) return "notFound";
  if (status === 409) return "conflict";
  if (status >= 500) return "server";
  if (status >= 400) return "validation";
  return "unexpected";
}

function extractCode(data: unknown): string | undefined {
  if (!data || typeof data !== "object") return undefined;

  const { code } = data as { code?: unknown };
  return typeof code === "string" ? code : undefined;
}

function extractMessages(data: unknown): string[] {
  if (!data || typeof data !== "object") return [];

  const body = data as { errors?: unknown; title?: unknown };

  if (Array.isArray(body.errors)) {
    return body.errors.filter((message): message is string => typeof message === "string");
  }

  if (body.errors && typeof body.errors === "object") {
    return Object.values(body.errors as Record<string, unknown>)
      .flat()
      .filter((message): message is string => typeof message === "string");
  }

  return typeof body.title === "string" ? [body.title] : [];
}

function isTimeout(cause: unknown): boolean {
  return (
    typeof cause === "object" &&
    cause !== null &&
    (cause as { name?: unknown }).name === "TimeoutError"
  );
}

function failure(
  error: AppError,
  method: string,
  path: string,
  cause?: unknown,
): { ok: false; error: AppError } {
  const status = error.status ? ` ${error.status}` : "";
  const code = error.code ? ` ${error.code}` : "";
  console.error(`[api] ${method} ${path} → ${error.kind}${status}${code}`, cause ?? error.messages);
  return err(error);
}

export type ApiFetchInit = Omit<RequestInit, "body" | "signal" | "headers"> & {
  body?: unknown;
  headers?: Record<string, string>;
  timeoutMs?: number;
};

export async function apiFetch<T>(
  path: string,
  init: ApiFetchInit = {},
): Promise<Result<T, AppError>> {
  const { body, headers, timeoutMs = DEFAULT_TIMEOUT_MS, ...rest } = init;
  const method = rest.method ?? "GET";

  let response: Response;

  try {
    response = await fetch(`${getBaseUrl()}${path}`, {
      ...rest,
      headers: { "Content-Type": "application/json", ...headers },
      body: body !== undefined ? JSON.stringify(body) : undefined,
      cache: "no-store",
      signal: AbortSignal.timeout(timeoutMs),
    });
  } catch (cause) {
    const kind = isTimeout(cause) ? "timeout" : "network";
    return failure({ kind, messages: [] }, method, path, cause);
  }

  if (response.status === 204) {
    return ok(undefined as T);
  }

  const data = await response.json().catch(() => null);

  if (!response.ok) {
    return failure(
      {
        kind: kindForStatus(response.status),
        status: response.status,
        code: extractCode(data),
        messages: extractMessages(data),
      },
      method,
      path,
    );
  }

  return ok(data as T);
}
