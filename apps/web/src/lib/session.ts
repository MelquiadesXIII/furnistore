import "server-only";

import { cookies } from "next/headers";
import { SESSION_COOKIE } from "@/lib/session-constants";
import { readActiveSessionClaims } from "@/lib/session-token";

async function readSession() {
  const token = (await cookies()).get(SESSION_COOKIE)?.value;
  const claims = readActiveSessionClaims(token);
  return token && claims ? { token, claims } : null;
}

export async function getSession(): Promise<string | null> {
  return (await readSession())?.token ?? null;
}

export async function getSessionUser(): Promise<{ email: string } | null> {
  const session = await readSession();
  if (!session) return null;

  const email = session.claims.email ?? session.claims.sub;
  return typeof email === "string" ? { email } : null;
}
