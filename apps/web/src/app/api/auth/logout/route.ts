import { NextResponse } from "next/server";
import { cookies } from "next/headers";
import { SESSION_COOKIE, REFRESH_COOKIE } from "@/lib/session-constants";
import { logout } from "@/modules/auth/api";

export async function POST() {
  const store = await cookies();
  const refreshToken = store.get(REFRESH_COOKIE)?.value;

  // Avisar a la API para que revoque el refresh token.
  // Si falla, seguimos: el usuario quiere salir igual.
  if (refreshToken) {
    await logout(refreshToken);
  }

  const response = NextResponse.json({ ok: true });
  response.cookies.delete(SESSION_COOKIE);
  response.cookies.delete(REFRESH_COOKIE);
  return response;
}