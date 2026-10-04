export const SESSION_COOKIE = "session";
export const REFRESH_COOKIE = "refresh_token";
export const SESSION_MAX_AGE = 60 * 60 * 24 * 30;
export const ADMIN_ROLE = "Admin";

export const sessionCookieOptions = {
  httpOnly: true,
  secure: process.env.NODE_ENV === "production",
  sameSite: "lax" as const,
  path: "/",
  maxAge: SESSION_MAX_AGE,
};
