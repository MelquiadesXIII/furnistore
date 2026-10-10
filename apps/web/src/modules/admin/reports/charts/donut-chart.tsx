"use client";

import { Pie, PieChart } from "recharts";
import { ChartContainer, ChartTooltip, type ChartConfig } from "@/components/ui/chart";
import { ReportTooltip } from "@/modules/admin/reports/charts/chart-tooltip";
import { toneColor } from "@/modules/admin/reports/charts/palette";
import type { ChartSpec } from "@/modules/admin/reports/document";
import { formatValue } from "@/modules/admin/reports/format";

export function DonutChart({ spec }: { spec: Extract<ChartSpec, { kind: "donut" }> }) {
  const data = spec.items.map((item) => ({ ...item, fill: toneColor(item.tone) }));
  const config = Object.fromEntries(
    data.map((item) => [item.label, { label: item.label, color: item.fill }]),
  ) satisfies ChartConfig;

  return (
    <div className="flex flex-col items-center gap-4 sm:flex-row">
      <ChartContainer config={config} className="aspect-square w-44 shrink-0">
        <PieChart>
          <ChartTooltip content={(props) => <ReportTooltip {...props} kind={spec.valueKind} />} />
          <Pie
            data={data}
            dataKey="value"
            nameKey="label"
            innerRadius="58%"
            outerRadius="100%"
            paddingAngle={1}
            stroke="var(--surface-raised)"
          />
        </PieChart>
      </ChartContainer>
      <ul className="flex w-full min-w-0 flex-col gap-2 text-sm">
        {data.map((item) => (
          <li key={item.label} className="flex items-center gap-2">
            <span className="size-2.5 shrink-0 rounded-[2px]" style={{ backgroundColor: item.fill }} />
            <span className="min-w-0 flex-1 truncate text-ink">{item.label}</span>
            <span className="font-mono text-xs text-ink-muted tabular-nums">
              {formatValue(spec.valueKind, item.value)}
            </span>
            <span className="w-14 text-right font-mono text-xs text-ink tabular-nums">
              {formatValue("percent", item.share)}
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}
