import type { Tone } from "@/modules/admin/reports/document";

export function toneColor(tone: Tone): string {
  return tone === "muted" ? "var(--ink-muted)" : `var(--${tone})`;
}
