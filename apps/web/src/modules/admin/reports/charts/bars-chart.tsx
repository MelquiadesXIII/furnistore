"use client";

import { Bar, BarChart, CartesianGrid, LabelList, Rectangle, XAxis, YAxis, type BarShapeProps } from "recharts";
import { ChartContainer, ChartTooltip, type ChartConfig } from "@/components/ui/chart";
import { ReportTooltip } from "@/modules/admin/reports/charts/chart-tooltip";
import { toneColor } from "@/modules/admin/reports/charts/palette";
import type { ChartSpec } from "@/modules/admin/reports/document";
import { formatAxisValue, formatValue } from "@/modules/admin/reports/format";

const LABEL_LIMIT = 24;

function truncate(text: string): string {
  return text.length > LABEL_LIMIT ? `${text.slice(0, LABEL_LIMIT - 1)}…` : text;
}

function ColoredBar(props: BarShapeProps) {
  const fill = (props.payload as { fill?: string } | undefined)?.fill ?? props.fill;
  return <Rectangle {...props} fill={fill} />;
}

export function BarsChart({ spec }: { spec: Extract<ChartSpec, { kind: "bars" }> }) {
  const config = { value: { label: spec.valueLabel, color: toneColor("chart-1") } } satisfies ChartConfig;
  const data = spec.items.map((item) => ({ label: item.label, value: item.value, fill: toneColor(item.tone) }));
  const horizontal = spec.layout === "horizontal";
  const height = horizontal ? Math.max(120, spec.items.length * 34 + 16) : 240;

  return (
    <ChartContainer config={config} className="aspect-auto w-full" style={{ height }}>
      <BarChart
        data={data}
        layout={horizontal ? "vertical" : "horizontal"}
        margin={horizontal ? { top: 0, right: 96, left: 0, bottom: 0 } : { top: 20, right: 8, left: 0, bottom: 0 }}
      >
        {horizontal ? (
          <>
            <XAxis type="number" hide />
            <YAxis
              type="category"
              dataKey="label"
              width={160}
              tickLine={false}
              axisLine={false}
              tickFormatter={(value) => truncate(String(value))}
            />
          </>
        ) : (
          <>
            <CartesianGrid vertical={false} />
            <XAxis dataKey="label" tickLine={false} axisLine={false} tickMargin={8} interval={0} />
            <YAxis
              tickLine={false}
              axisLine={false}
              width={60}
              tickFormatter={(value) => formatAxisValue(spec.valueKind, Number(value))}
            />
          </>
        )}
        <ChartTooltip cursor={false} content={(props) => <ReportTooltip {...props} kind={spec.valueKind} />} />
        <Bar
          dataKey="value"
          name={spec.valueLabel}
          shape={ColoredBar}
          radius={horizontal ? [0, 2, 2, 0] : [2, 2, 0, 0]}
          maxBarSize={horizontal ? 22 : 48}
        >
          <LabelList
            dataKey="value"
            position={horizontal ? "right" : "top"}
            className="fill-ink font-mono"
            fontSize={11}
            formatter={(value) => formatValue(spec.valueKind, Number(value))}
          />
        </Bar>
      </BarChart>
    </ChartContainer>
  );
}
