const ROUTE_ID = /^[1-9]\d{0,9}$/;
const MAX_ID = 2_147_483_647;

export function parseRouteId(value: string): number | null {
  if (!ROUTE_ID.test(value)) return null;
  const id = Number(value);
  return id <= MAX_ID ? id : null;
}
