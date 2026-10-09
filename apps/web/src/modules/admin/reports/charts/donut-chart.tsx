"use client";

import { Pie, PieChart } from "recharts";
import { ChartContainer, ChartTooltip, type ChartConfig } from "@/components/ui/chart";
import { ReportTooltip } from "@/modules/admin/reports/charts/chart-tooltip";
import { formatValue } from "@/modules/admin/reports/format";

export type DonutItem = { label: string; value: number; share: number; color: string };

export function ReportDonutChart({ items }: { items: DonutItem[] }) {
  const data = items.map((item) => ({ ...item, fill: item.color }));
  const config = Object.fromEntries(
    items.map((item) => [item.label, { label: item.label, color: item.color }]),
  ) satisfies ChartConfig;

  return (
    <div className="flex flex-col items-center gap-6 sm:flex-row">
      <ChartContainer config={config} className="aspect-square w-44 shrink-0">
        <PieChart>
          <ChartTooltip content={(props) => <ReportTooltip {...props} kind="count" />} />
          <Pie data={data} dataKey="value" nameKey="label" innerRadius="55%" outerRadius="100%" />
        </PieChart>
      </ChartContainer>
      <ul className="flex flex-col gap-2 text-sm">
        {items.map((item) => (
          <li key={item.label} className="flex items-center gap-2">
            <span className="size-3 rounded-sm" style={{ backgroundColor: item.color }} />
            <span className="text-ink">{item.label}</span>
            <span className="text-ink-muted">
              {formatValue("count", item.value)} ({formatValue("percent", item.share)})
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}
