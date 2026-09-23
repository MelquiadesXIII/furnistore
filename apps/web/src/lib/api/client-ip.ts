import "server-only";

export function clientIpFrom(headers: Pick<Headers, "get">): string | undefined {
  const hops = headers
    .get("x-forwarded-for")
    ?.split(",")
    .map((hop) => hop.trim())
    .filter(Boolean);

  return hops?.at(-1);
}
