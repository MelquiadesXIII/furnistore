import { formatPrice } from "@/lib/format-price";
import type { ReportGrouping } from "@/modules/admin/reports/types";

export type ValueKind = "money" | "count" | "percent" | "hours" | "days" | "decimal";

const COUNT = new Intl.NumberFormat("es");
const DECIMAL = new Intl.NumberFormat("es", { maximumFractionDigits: 2 });
const ONE_DECIMAL = new Intl.NumberFormat("es", { maximumFractionDigits: 1 });
const PERCENT = new Intl.NumberFormat("es", {
  style: "percent",
  minimumFractionDigits: 1,
  maximumFractionDigits: 1,
});
const CHANGE = new Intl.NumberFormat("es", {
  style: "percent",
  maximumFractionDigits: 1,
  signDisplay: "exceptZero",
});
const COMPACT = new Intl.NumberFormat("es", { notation: "compact", maximumFractionDigits: 1 });

const DAY_MONTH = new Intl.DateTimeFormat("es", { day: "numeric", month: "short", timeZone: "UTC" });
const DAY_MONTH_YEAR = new Intl.DateTimeFormat("es", {
  day: "numeric",
  month: "short",
  year: "numeric",
  timeZone: "UTC",
});
const MONTH_YEAR = new Intl.DateTimeFormat("es", { month: "short", year: "numeric", timeZone: "UTC" });
const DAY_ONLY = new Intl.DateTimeFormat("es", { day: "numeric", timeZone: "UTC" });

const HOURS_PER_DAY = 24;

function day(isoDate: string): Date {
  return new Date(`${isoDate}T00:00:00Z`);
}

function isMonthEnd(isoDate: string): boolean {
  const next = day(isoDate);
  next.setUTCDate(next.getUTCDate() + 1);
  return next.getUTCDate() === 1;
}

function clean(text: string): string {
  return text.replace(/\.(?=\s|$)/g, "");
}

export function formatHours(hours: number): string {
  return hours < 2 * HOURS_PER_DAY
    ? `${ONE_DECIMAL.format(hours)} h`
    : `${ONE_DECIMAL.format(hours / HOURS_PER_DAY)} días`;
}

export function formatValue(kind: ValueKind, value: number | null | undefined): string {
  if (value === null || value === undefined) return "—";

  switch (kind) {
    case "money":
      return formatPrice(value);
    case "count":
      return COUNT.format(value);
    case "percent":
      return PERCENT.format(value);
    case "hours":
      return formatHours(value);
    case "days":
      return `${ONE_DECIMAL.format(value)} ${value === 1 ? "día" : "días"}`;
    case "decimal":
      return DECIMAL.format(value);
  }
}

export function formatAxisValue(kind: ValueKind, value: number): string {
  if (kind === "money") return `$${COMPACT.format(value)}`;
  if (kind === "percent") return PERCENT.format(value);
  if (kind === "hours") return formatHours(value);
  return COMPACT.format(value);
}

export function formatChange(change: number | null): string {
  return change === null ? "sin base de comparación" : CHANGE.format(change);
}

export function formatDay(isoDate: string): string {
  return clean(DAY_MONTH_YEAR.format(day(isoDate)));
}

export function formatRange(from: string, to: string): string {
  if (from === to) return formatDay(from);

  const sameYear = from.slice(0, 4) === to.slice(0, 4);
  const start = sameYear ? DAY_MONTH.format(day(from)) : DAY_MONTH_YEAR.format(day(from));
  return clean(`${start} – ${DAY_MONTH_YEAR.format(day(to))}`);
}

export function formatBucket(start: string, end: string, grouping: ReportGrouping): string {
  if (grouping === "Day" || start === end) return clean(DAY_MONTH.format(day(start)));

  if (grouping === "Month" && start.endsWith("-01") && isMonthEnd(end)) {
    return clean(MONTH_YEAR.format(day(start)));
  }

  if (start.slice(0, 7) === end.slice(0, 7)) {
    return clean(`${DAY_ONLY.format(day(start))}–${DAY_MONTH.format(day(end))}`);
  }

  return clean(`${DAY_MONTH.format(day(start))} – ${DAY_MONTH.format(day(end))}`);
}

export const GROUPING_LABELS: Record<ReportGrouping, string> = {
  Day: "Por día",
  Week: "Por semana",
  Month: "Por mes",
};
