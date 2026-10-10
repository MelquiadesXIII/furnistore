"use client";

import { Bar, BarChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { ChartContainer, ChartTooltip, type ChartConfig } from "@/components/ui/chart";
import { ChartKey } from "@/modules/admin/reports/charts/chart-key";
import { ReportTooltip } from "@/modules/admin/reports/charts/chart-tooltip";
import { toneColor } from "@/modules/admin/reports/charts/palette";
import type { ChartSpec } from "@/modules/admin/reports/document";
import { formatAxisValue } from "@/modules/admin/reports/format";

export function StackedChart({ spec }: { spec: Extract<ChartSpec, { kind: "stacked" }> }) {
  const keys = spec.series.map((entry, index) => ({
    key: `s${index}`,
    label: entry.label,
    color: toneColor(entry.tone),
  }));
  const config = Object.fromEntries(
    keys.map(({ key, label, color }) => [key, { label, color }]),
  ) satisfies ChartConfig;
  const data = spec.points.map((point) => ({
    label: point.label,
    ...Object.fromEntries(keys.map(({ key }, index) => [key, point.values[index] ?? 0])),
  }));

  return (
    <div className="flex flex-col gap-3">
      <ChartContainer config={config} className="aspect-auto w-full" style={{ height: 240 }}>
        <BarChart data={data} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
          <CartesianGrid vertical={false} />
          <XAxis dataKey="label" tickLine={false} axisLine={false} tickMargin={8} minTickGap={12} />
          <YAxis
            tickLine={false}
            axisLine={false}
            width={60}
            tickFormatter={(value) => formatAxisValue(spec.valueKind, Number(value))}
          />
          <ChartTooltip content={(props) => <ReportTooltip {...props} kind={spec.valueKind} />} />
          {keys.map(({ key, label, color }, index) => (
            <Bar
              key={key}
              dataKey={key}
              name={label}
              stackId="total"
              fill={color}
              maxBarSize={36}
              radius={index === keys.length - 1 ? [2, 2, 0, 0] : 0}
            />
          ))}
        </BarChart>
      </ChartContainer>
      <ChartKey items={keys.map(({ label, color }) => ({ label, color }))} />
    </div>
  );
}
