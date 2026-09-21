export type SessionClaims = {
  email?: unknown;
  sub?: unknown;
  exp?: unknown;
};

export function readActiveSessionClaims(token: string | undefined): SessionClaims | null {
  const segment = token?.split(".")[1];
  if (!segment) return null;

  try {
    const base64 = segment.replace(/-/g, "+").replace(/_/g, "/");
    const binary = atob(base64.padEnd(Math.ceil(base64.length / 4) * 4, "="));
    const claims: unknown = JSON.parse(
      new TextDecoder().decode(Uint8Array.from(binary, (char) => char.charCodeAt(0))),
    );

    if (typeof claims !== "object" || claims === null) return null;

    const { exp } = claims as SessionClaims;
    return typeof exp === "number" && exp * 1000 > Date.now() ? (claims as SessionClaims) : null;
  } catch {
    return null;
  }
}
