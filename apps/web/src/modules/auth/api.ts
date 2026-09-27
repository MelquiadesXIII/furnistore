import { apiFetch } from "@/lib/api/client";
import type { ApiSchemas } from "@/lib/api/contract";
import type { Result } from "@/lib/result";
import type { AuthTokens, RegisterResult } from "@/modules/auth/types";

const EMAIL_TIMEOUT_MS = 20_000;

function forwardedFor(clientIp: string | undefined): Record<string, string> {
  return clientIp ? { "X-Forwarded-For": clientIp } : {};
}

export function login(
  request: ApiSchemas["LoginRequest"],
  clientIp?: string,
): Promise<Result<AuthTokens>> {
  return apiFetch<AuthTokens>("/api/authentication/login", {
    method: "POST",
    body: request,
    headers: forwardedFor(clientIp),
  });
}

export function register(
  request: ApiSchemas["RegisterRequest"],
  clientIp?: string,
): Promise<Result<RegisterResult>> {
  return apiFetch<RegisterResult>("/api/authentication/register", {
    method: "POST",
    body: request,
    headers: forwardedFor(clientIp),
    timeoutMs: EMAIL_TIMEOUT_MS,
  });
}

export function resendConfirmation(
  request: ApiSchemas["ResendConfirmationRequest"],
  clientIp?: string,
): Promise<Result<void>> {
  return apiFetch<void>("/api/authentication/resend-confirmation", {
    method: "POST",
    body: request,
    headers: forwardedFor(clientIp),
    timeoutMs: EMAIL_TIMEOUT_MS,
  });
}

export function verifyEmail(
  request: ApiSchemas["VerifyEmailRequest"],
  clientIp?: string,
): Promise<Result<void>> {
  return apiFetch<void>("/api/authentication/verify-email", {
    method: "POST",
    body: request,
    headers: forwardedFor(clientIp),
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

export function logout(refreshToken: string, clientIp?: string): Promise<Result<void>> {
  return apiFetch<void>("/api/authentication/logout", {
    method: "POST",
    body: { refreshToken } satisfies ApiSchemas["LogoutRequest"],
    headers: forwardedFor(clientIp),
  });
}
