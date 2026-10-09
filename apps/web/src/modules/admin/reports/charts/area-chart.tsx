"use client";

import { Area, AreaChart, CartesianGrid, XAxis, YAxis } from "recharts";
import { ChartContainer, ChartTooltip, type ChartConfig } from "@/components/ui/chart";
import { ReportTooltip } from "@/modules/admin/reports/charts/chart-tooltip";
import { formatAxisValue, type ValueKind } from "@/modules/admin/reports/format";

export function ReportAreaChart({
  points,
  kind,
  label,
}: {
  points: { label: string; value: number }[];
  kind: ValueKind;
  label: string;
}) {
  const config = { value: { label, color: "var(--chart-1)" } } satisfies ChartConfig;

  return (
    <ChartContainer config={config} className="aspect-auto h-64 w-full">
      <AreaChart data={points} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
        <CartesianGrid vertical={false} />
        <XAxis dataKey="label" tickLine={false} axisLine={false} tickMargin={8} minTickGap={12} />
        <YAxis
          tickLine={false}
          axisLine={false}
          width={60}
          tickFormatter={(value) => formatAxisValue(kind, Number(value))}
        />
        <ChartTooltip content={(props) => <ReportTooltip {...props} kind={kind} />} />
        <Area
          dataKey="value"
          name={label}
          type="monotone"
          stroke="var(--color-value)"
          fill="var(--color-value)"
          fillOpacity={0.2}
          strokeWidth={2}
        />
      </AreaChart>
    </ChartContainer>
  );
}
