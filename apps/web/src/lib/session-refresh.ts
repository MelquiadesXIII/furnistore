import type { ApiSchemas } from "@/lib/api/contract";

export type RefreshOutcome =
  | { kind: "renewed"; token: string; refreshToken: string }
  | { kind: "rejected" }
  | { kind: "unavailable" };

const REFRESH_TIMEOUT_MS = 5000;

export async function refreshSession(
  token: string,
  refreshToken: string,
  clientIp: string | undefined,
): Promise<RefreshOutcome> {
  const baseUrl = process.env.API_BASE_URL;
  if (!baseUrl) return { kind: "unavailable" };

  try {
    const response = await fetch(`${baseUrl}/api/authentication/refresh-token`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        ...(clientIp ? { "X-Forwarded-For": clientIp } : {}),
      },
      body: JSON.stringify({ token, refreshToken } satisfies ApiSchemas["RefreshTokenRequest"]),
      cache: "no-store",
      signal: AbortSignal.timeout(REFRESH_TIMEOUT_MS),
    });

    if (response.status === 401 || response.status === 403) return { kind: "rejected" };
    if (!response.ok) return { kind: "unavailable" };

    const tokens = (await response.json()) as ApiSchemas["AuthTokensResponse"];
    return { kind: "renewed", token: tokens.token, refreshToken: tokens.refreshToken };
  } catch {
    return { kind: "unavailable" };
  }
}
