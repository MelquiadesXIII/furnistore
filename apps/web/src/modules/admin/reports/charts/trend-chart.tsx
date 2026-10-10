"use client";

import { Area, Bar, CartesianGrid, ComposedChart, Line, XAxis, YAxis } from "recharts";
import { ChartContainer, ChartTooltip, type ChartConfig } from "@/components/ui/chart";
import { ChartKey } from "@/modules/admin/reports/charts/chart-key";
import { ReportTooltip } from "@/modules/admin/reports/charts/chart-tooltip";
import { toneColor } from "@/modules/admin/reports/charts/palette";
import type { ChartSpec } from "@/modules/admin/reports/document";
import { formatAxisValue } from "@/modules/admin/reports/format";

const CURRENT_COLOR = toneColor("chart-1");
const PREVIOUS_COLOR = toneColor("muted");

export function TrendChart({ spec }: { spec: Extract<ChartSpec, { kind: "trend" }> }) {
  const config = {
    value: { label: spec.valueLabel, color: CURRENT_COLOR },
    previous: { label: spec.previousLabel, color: PREVIOUS_COLOR },
  } satisfies ChartConfig;

  return (
    <div className="flex flex-col gap-3">
      <ChartContainer
        config={config}
        className="aspect-auto w-full"
        style={{ height: spec.variant === "area" ? 260 : 200 }}
      >
        <ComposedChart data={spec.points} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
          <CartesianGrid vertical={false} />
          <XAxis dataKey="label" tickLine={false} axisLine={false} tickMargin={8} minTickGap={12} />
          <YAxis
            tickLine={false}
            axisLine={false}
            width={60}
            tickFormatter={(value) => formatAxisValue(spec.valueKind, Number(value))}
          />
          <ChartTooltip content={(props) => <ReportTooltip {...props} kind={spec.valueKind} />} />
          {spec.variant === "area" ? (
            <Area
              dataKey="value"
              name={spec.valueLabel}
              type="monotone"
              stroke="var(--color-value)"
              fill="var(--color-value)"
              fillOpacity={0.18}
              strokeWidth={2}
            />
          ) : (
            <Bar dataKey="value" name={spec.valueLabel} fill="var(--color-value)" radius={[2, 2, 0, 0]} maxBarSize={36} />
          )}
          <Line
            dataKey="previous"
            name={spec.previousLabel}
            type="monotone"
            stroke="var(--color-previous)"
            strokeDasharray="4 4"
            strokeWidth={1.5}
            dot={false}
            connectNulls
          />
        </ComposedChart>
      </ChartContainer>
      <ChartKey
        items={[
          { label: spec.valueLabel, color: CURRENT_COLOR },
          { label: spec.previousLabel, color: PREVIOUS_COLOR, dashed: true },
        ]}
      />
    </div>
  );
}
