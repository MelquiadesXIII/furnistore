type QueryValue = string | number | boolean | null | undefined;

export function toQueryString(params: Record<string, QueryValue>): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") search.set(key, String(value));
  }

  const query = search.toString();
  return query ? `?${query}` : "";
}
