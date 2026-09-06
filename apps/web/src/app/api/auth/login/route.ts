import { NextResponse } from "next/server";
import { toUserMessage } from "@/lib/errors";
import { SESSION_COOKIE, sessionCookieOptions } from "@/lib/session-constants";
import { login } from "@/modules/auth/api";

export async function POST(request: Request) {
  const { email, password } = await request.json();

  const result = await login(email, password);

  if (!result.ok) {
    const { error } = result;
    return NextResponse.json(
      { errors: error.messages.length > 0 ? error.messages : [toUserMessage(error)] },
      { status: error.status ?? 502 },
    );
  }

  const response = NextResponse.json({ ok: true });
  response.cookies.set(SESSION_COOKIE, result.value.token, sessionCookieOptions);
  return response;
}
