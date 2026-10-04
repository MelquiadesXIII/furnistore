import { NextResponse, type NextRequest } from "next/server";
import { REFRESH_COOKIE, SESSION_COOKIE, sessionCookieOptions } from "@/lib/session-constants";
import { refreshSession } from "@/lib/session-refresh";
import { readSessionClaims, secondsUntilExpiry } from "@/lib/session-token";

const PRIVATE_PATHS = [/^\/cart$/, /^\/checkout$/, /^\/orders(\/.*)?$/, /^\/profile$/, /^\/admin(\/.*)?$/];
const RENEW_BEFORE_SECONDS = 120;

function isPrivate(pathname: string): boolean {
  return PRIVATE_PATHS.some((pattern) => pattern.test(pathname));
}

function clientIp(request: NextRequest): string | undefined {
  return request.headers.get("x-forwarded-for")?.split(",").at(-1)?.trim() || undefined;
}

function toLogin(request: NextRequest): NextResponse {
  const { pathname, search } = request.nextUrl;
  const loginUrl = new URL("/login", request.url);
  loginUrl.searchParams.set("next", `${pathname}${search}`);
  return NextResponse.redirect(loginUrl);
}

function clearSession(response: NextResponse): NextResponse {
  response.cookies.delete({ name: SESSION_COOKIE, path: "/" });
  response.cookies.delete({ name: REFRESH_COOKIE, path: "/" });
  return response;
}

export async function middleware(request: NextRequest) {
  const token = request.cookies.get(SESSION_COOKIE)?.value;
  const refreshToken = request.cookies.get(REFRESH_COOKIE)?.value;
  const remaining = secondsUntilExpiry(readSessionClaims(token));
  const privatePath = isPrivate(request.nextUrl.pathname);

  if (remaining > RENEW_BEFORE_SECONDS || !token || !refreshToken) {
    if (remaining > 0) return NextResponse.next();
    return privatePath ? toLogin(request) : NextResponse.next();
  }

  const outcome = await refreshSession(token, refreshToken, clientIp(request));

  if (outcome.kind === "renewed") {
    request.cookies.set(SESSION_COOKIE, outcome.token);
    request.cookies.set(REFRESH_COOKIE, outcome.refreshToken);

    const response = NextResponse.next({ request: { headers: request.headers } });
    response.cookies.set(SESSION_COOKIE, outcome.token, sessionCookieOptions);
    response.cookies.set(REFRESH_COOKIE, outcome.refreshToken, sessionCookieOptions);
    return response;
  }

  if (remaining > 0) return NextResponse.next();

  if (outcome.kind === "rejected") {
    return clearSession(privatePath ? toLogin(request) : NextResponse.next());
  }

  return privatePath ? toLogin(request) : NextResponse.next();
}

export const config = {
  runtime: "nodejs",
  matcher: ["/((?!api/|_next/static|_next/image|favicon.ico|.*\\.(?:png|jpg|jpeg|svg|webp|ico)$).*)"],
};
