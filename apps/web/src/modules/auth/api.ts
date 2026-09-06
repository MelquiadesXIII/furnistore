import { apiFetch } from "@/lib/api/client";
import type { Result } from "@/lib/result";
import type { AuthTokens, RegisterResult } from "@/modules/auth/types";

export function login(email: string, password: string): Promise<Result<AuthTokens>> {
  return apiFetch<AuthTokens>("/api/authentication/login", {
    method: "POST",
    body: { email, password },
  });
}

export function register(
  name: string,
  emailAddress: string,
  password: string,
): Promise<Result<RegisterResult>> {
  return apiFetch<RegisterResult>("/api/authentication/register", {
    method: "POST",
    body: { name, emailAddress, password },
  });
}

export function refreshToken(
  token: string,
  refreshTokenValue: string,
): Promise<Result<AuthTokens>> {
  return apiFetch<AuthTokens>("/api/authentication/refresh-token", {
    method: "POST",
    body: { token, refreshToken: refreshTokenValue },
  });
}
