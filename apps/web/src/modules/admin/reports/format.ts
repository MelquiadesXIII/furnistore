import { formatPrice } from "@/lib/format-price";
import type { ReportGrouping } from "@/modules/admin/reports/types";

export type ValueKind = "money" | "count" | "percent";

const COUNT = new Intl.NumberFormat("es");
const PERCENT = new Intl.NumberFormat("es", { style: "percent", maximumFractionDigits: 1 });
const COMPACT = new Intl.NumberFormat("es", { notation: "compact", maximumFractionDigits: 1 });

const DAY_MONTH = new Intl.DateTimeFormat("es", { day: "numeric", month: "short", timeZone: "UTC" });
const DAY_MONTH_YEAR = new Intl.DateTimeFormat("es", {
  day: "numeric",
  month: "short",
  year: "numeric",
  timeZone: "UTC",
});
const MONTH_YEAR = new Intl.DateTimeFormat("es", { month: "short", year: "numeric", timeZone: "UTC" });

function day(isoDate: string): Date {
  return new Date(`${isoDate}T00:00:00Z`);
}

export function formatValue(kind: ValueKind, value: number | null | undefined): string {
  if (value === null || value === undefined) return "—";
  if (kind === "money") return formatPrice(value);
  if (kind === "percent") return PERCENT.format(value);
  return COUNT.format(value);
}

export function formatAxisValue(kind: ValueKind, value: number): string {
  return kind === "money" ? `$${COMPACT.format(value)}` : COMPACT.format(value);
}

export function formatRange(from: string, to: string): string {
  return `${DAY_MONTH_YEAR.format(day(from))} – ${DAY_MONTH_YEAR.format(day(to))}`;
}

export function formatBucket(start: string, end: string, grouping: ReportGrouping): string {
  if (grouping === "Month") return MONTH_YEAR.format(day(start));
  if (grouping === "Week") return `${DAY_MONTH.format(day(start))} – ${DAY_MONTH.format(day(end))}`;
  return DAY_MONTH.format(day(start));
}

export const GROUPING_LABELS: Record<ReportGrouping, string> = {
  Day: "Por día",
  Week: "Por semana",
  Month: "Por mes",
};
