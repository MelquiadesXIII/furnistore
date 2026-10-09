import { STORE_TIME_ZONE } from "@/lib/format-date";

export type DatePreset = { id: string; label: string; from: string; to: string };

const TODAY = new Intl.DateTimeFormat("en-CA", {
  timeZone: STORE_TIME_ZONE,
  year: "numeric",
  month: "2-digit",
  day: "2-digit",
});

function toIso(date: Date): string {
  return date.toISOString().slice(0, 10);
}

function shiftDays(isoDate: string, days: number): string {
  const date = new Date(`${isoDate}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return toIso(date);
}

export function storeToday(): string {
  return TODAY.format(new Date());
}

export function datePresets(today: string): DatePreset[] {
  const [year, month] = today.split("-").map(Number);
  const firstOfMonth = `${today.slice(0, 7)}-01`;
  const lastMonthEnd = shiftDays(firstOfMonth, -1);
  const lastMonthStart = `${lastMonthEnd.slice(0, 7)}-01`;

  return [
    { id: "7d", label: "Últimos 7 días", from: shiftDays(today, -6), to: today },
    { id: "30d", label: "Últimos 30 días", from: shiftDays(today, -29), to: today },
    { id: "90d", label: "Últimos 90 días", from: shiftDays(today, -89), to: today },
    { id: "month", label: "Este mes", from: firstOfMonth, to: today },
    { id: "last-month", label: "Mes anterior", from: lastMonthStart, to: lastMonthEnd },
    {
      id: "quarter",
      label: "Este trimestre",
      from: `${year}-${String(Math.floor((month - 1) / 3) * 3 + 1).padStart(2, "0")}-01`,
      to: today,
    },
    { id: "year", label: "Este año", from: `${year}-01-01`, to: today },
    { id: "12m", label: "Últimos 12 meses", from: shiftDays(today, -364), to: today },
  ];
}
