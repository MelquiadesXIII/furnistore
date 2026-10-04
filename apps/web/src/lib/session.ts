import "server-only";

import { cookies } from "next/headers";
import { ADMIN_ROLE, SESSION_COOKIE } from "@/lib/session-constants";
import { readActiveSessionClaims, rolesOf } from "@/lib/session-token";

export type SessionUser = { email: string; isAdmin: boolean };

async function readSession() {
  const token = (await cookies()).get(SESSION_COOKIE)?.value;
  const claims = readActiveSessionClaims(token);
  return token && claims ? { token, claims } : null;
}

export async function getSession(): Promise<string | null> {
  return (await readSession())?.token ?? null;
}

export async function getSessionUser(): Promise<SessionUser | null> {
  const session = await readSession();
  if (!session) return null;

  const email = session.claims.email ?? session.claims.sub;
  return typeof email === "string"
    ? { email, isAdmin: rolesOf(session.claims).includes(ADMIN_ROLE) }
    : null;
}
