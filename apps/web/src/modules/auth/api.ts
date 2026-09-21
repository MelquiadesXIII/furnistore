import { apiFetch } from "@/lib/api/client";
import type { ApiSchemas } from "@/lib/api/contract";
import type { Result } from "@/lib/result";
import type { AuthTokens, RegisterResult } from "@/modules/auth/types";

export function login(email: string, password: string): Promise<Result<AuthTokens>> {
  return apiFetch<AuthTokens>("/api/authentication/login", {
    method: "POST",
    body: { email, password } satisfies ApiSchemas["LoginRequest"],
  });
}

export function register(request: ApiSchemas["RegisterRequest"]): Promise<Result<RegisterResult>> {
  return apiFetch<RegisterResult>("/api/authentication/register", {
    method: "POST",
    body: request,
  });
}

export function refreshToken(
  token: string,
  refreshTokenValue: string,
): Promise<Result<AuthTokens>> {
  return apiFetch<AuthTokens>("/api/authentication/refresh-token", {
    method: "POST",
    body: { token, refreshToken: refreshTokenValue } satisfies ApiSchemas["RefreshTokenRequest"],
  });
}

export function logout(refreshToken: string): Promise<Result<void>> {
  return apiFetch<void>("/api/authentication/logout", {
    method: "POST",
    body: { refreshToken } satisfies ApiSchemas["LogoutRequest"],
  });
}
