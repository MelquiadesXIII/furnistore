import type { AuthFailure } from "@/modules/auth/types";

export type SignInResult = { ok: true } | { ok: false; failure: AuthFailure };

export const UNREACHABLE: AuthFailure = {
  code: null,
  message: "No se pudo conectar con el servidor. Intenta de nuevo en un momento.",
};

export async function signIn(email: string, password: string): Promise<SignInResult> {
  const res = await fetch("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  }).catch(() => null);

  if (res?.ok) return { ok: true };

  const data = await res?.json().catch(() => null);
  return { ok: false, failure: data?.error ?? UNREACHABLE };
}
