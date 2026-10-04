export type SessionClaims = {
  email?: unknown;
  sub?: unknown;
  exp?: unknown;
  role?: unknown;
};

const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

export function readSessionClaims(token: string | undefined): SessionClaims | null {
  const segment = token?.split(".")[1];
  if (!segment) return null;

  try {
    const base64 = segment.replace(/-/g, "+").replace(/_/g, "/");
    const binary = atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, "="));
    const claims: unknown = JSON.parse(
      new TextDecoder().decode(Uint8Array.from(binary, (char) => char.charCodeAt(0))),
    );

    return typeof claims === "object" && claims !== null ? (claims as SessionClaims) : null;
  } catch {
    return null;
  }
}

export function secondsUntilExpiry(claims: SessionClaims | null): number {
  const exp = claims?.exp;
  return typeof exp === "number" ? exp - Date.now() / 1000 : -Infinity;
}

export function readActiveSessionClaims(token: string | undefined): SessionClaims | null {
  const claims = readSessionClaims(token);
  return secondsUntilExpiry(claims) > 0 ? claims : null;
}

export function rolesOf(claims: SessionClaims): string[] {
  const raw = claims.role ?? (claims as Record<string, unknown>)[ROLE_CLAIM];
  const roles = Array.isArray(raw) ? raw : [raw];
  return roles.filter((role): role is string => typeof role === "string");
}
