export const STORE_TIME_ZONE = "America/Havana";

const DAY = new Intl.DateTimeFormat("es", {
  day: "numeric",
  month: "long",
  year: "numeric",
  timeZone: "UTC",
});

const MOMENT = new Intl.DateTimeFormat("es", {
  day: "numeric",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  timeZone: STORE_TIME_ZONE,
});

const MOMENT_DAY = new Intl.DateTimeFormat("es", {
  day: "numeric",
  month: "long",
  year: "numeric",
  timeZone: STORE_TIME_ZONE,
});

export function formatCalendarDate(isoDate: string): string {
  return DAY.format(new Date(`${isoDate}T00:00:00Z`));
}

export function formatMoment(isoDateTime: string): string {
  return MOMENT.format(new Date(isoDateTime));
}

export function formatMomentDay(isoDateTime: string): string {
  return MOMENT_DAY.format(new Date(isoDateTime));
}
