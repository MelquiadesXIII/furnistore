"use client";

import type { TooltipContentProps } from "recharts";
import { formatValue, type ValueKind } from "@/modules/admin/reports/format";

export function ReportTooltip({
  active,
  payload,
  label,
  kind,
}: TooltipContentProps & { kind: ValueKind }) {
  if (!active || !payload?.length) return null;

  const heading = label;

  return (
    <div className="grid min-w-40 gap-1.5 rounded-sm border border-hairline bg-surface-raised px-3 py-2 text-xs shadow-lg">
      {heading !== undefined && heading !== "" && <span className="font-medium text-ink">{String(heading)}</span>}
      {payload
        .filter((item) => item.value !== null && item.value !== undefined)
        .map((item) => (
          <div key={String(item.dataKey ?? item.name)} className="flex items-center justify-between gap-4">
            <span className="flex items-center gap-1.5 text-ink-muted">
              <span
                className="size-2.5 shrink-0 rounded-[2px]"
                style={{ backgroundColor: item.color ?? item.payload?.fill }}
              />
              {item.name}
            </span>
            <span className="font-mono text-ink tabular-nums">{formatValue(kind, Number(item.value))}</span>
          </div>
        ))}
    </div>
  );
}
