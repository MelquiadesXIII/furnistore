const ORIGIN = "http://furnistore.local";

export function safeNextPath(value: unknown): string {
  if (typeof value !== "string" || !value.startsWith("/")) return "/";

  try {
    const url = new URL(value, ORIGIN);
    return url.origin === ORIGIN ? `${url.pathname}${url.search}${url.hash}` : "/";
  } catch {
    return "/";
  }
}
