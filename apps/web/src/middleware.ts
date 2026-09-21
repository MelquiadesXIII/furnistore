import { NextResponse, type NextRequest } from "next/server";
import { SESSION_COOKIE } from "@/lib/session-constants";
import { readActiveSessionClaims } from "@/lib/session-token";

export function middleware(request: NextRequest) {
  const token = request.cookies.get(SESSION_COOKIE)?.value;

  if (readActiveSessionClaims(token)) {
    return NextResponse.next();
  }

  const { pathname, search } = request.nextUrl;
  const loginUrl = new URL("/login", request.url);
  loginUrl.searchParams.set("next", `${pathname}${search}`);

  const response = NextResponse.redirect(loginUrl);
  if (token) response.cookies.delete({ name: SESSION_COOKIE, path: "/" });
  return response;
}

export const config = {
  matcher: ["/cart", "/checkout", "/orders/:path*", "/profile"],
};
