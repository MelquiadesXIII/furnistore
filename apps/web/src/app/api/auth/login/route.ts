// apps/web/src/app/api/auth/login/route.ts
import { NextResponse } from "next/server";
import { clientIpFrom } from "@/lib/api/client-ip";
import { SESSION_COOKIE, REFRESH_COOKIE, sessionCookieOptions } from "@/lib/session-constants";
import { login } from "@/modules/auth/api";
import { toAuthFailure } from "@/modules/auth/error-messages";

export async function POST(request: Request) {
  const { email, password } = await request.json().catch(() => ({}));
  const result = await login({ email, password }, clientIpFrom(request.headers));

  if (!result.ok) {
    return NextResponse.json(
      { error: toAuthFailure(result.error) },
      { status: result.error.status ?? 502 },
    );
  }

  const response = NextResponse.json({ ok: true });
  response.cookies.set(SESSION_COOKIE, result.value.token, sessionCookieOptions);
  response.cookies.set(REFRESH_COOKIE, result.value.refreshToken, sessionCookieOptions);
  return response;
}
