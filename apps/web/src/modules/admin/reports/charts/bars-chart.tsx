"use client";

import { Bar, BarChart, CartesianGrid, LabelList, Rectangle, XAxis, YAxis, type BarShapeProps } from "recharts";
import { ChartContainer, ChartTooltip, type ChartConfig } from "@/components/ui/chart";
import { ReportTooltip } from "@/modules/admin/reports/charts/chart-tooltip";
import { formatAxisValue, formatValue, type ValueKind } from "@/modules/admin/reports/format";

export type BarItem = { label: string; value: number; color?: string };

function ColoredBar(props: BarShapeProps) {
  const fill = (props.payload as BarItem | undefined)?.color ?? props.fill;
  return <Rectangle {...props} fill={fill} />;
}

export function ReportBarsChart({
  items,
  kind,
  label,
  horizontal = false,
}: {
  items: BarItem[];
  kind: ValueKind;
  label: string;
  horizontal?: boolean;
}) {
  const config = { value: { label, color: "var(--chart-1)" } } satisfies ChartConfig;
  const height = horizontal ? Math.max(120, items.length * 34) : 240;

  return (
    <ChartContainer config={config} className="aspect-auto w-full" style={{ height }}>
      <BarChart
        data={items}
        layout={horizontal ? "vertical" : "horizontal"}
        margin={horizontal ? { top: 0, right: 96, left: 0, bottom: 0 } : { top: 20, right: 8, left: 0, bottom: 0 }}
      >
        {horizontal ? (
          <>
            <XAxis type="number" hide />
            <YAxis type="category" dataKey="label" width={160} tickLine={false} axisLine={false} />
          </>
        ) : (
          <>
            <CartesianGrid vertical={false} />
            <XAxis dataKey="label" tickLine={false} axisLine={false} tickMargin={8} interval={0} />
            <YAxis
              tickLine={false}
              axisLine={false}
              width={60}
              tickFormatter={(value) => formatAxisValue(kind, Number(value))}
            />
          </>
        )}
        <ChartTooltip cursor={false} content={(props) => <ReportTooltip {...props} kind={kind} />} />
        <Bar dataKey="value" name={label} fill="var(--color-value)" shape={ColoredBar} maxBarSize={horizontal ? 22 : 48}>
          <LabelList
            dataKey="value"
            position={horizontal ? "right" : "top"}
            className="fill-ink"
            fontSize={11}
            formatter={(value) => formatValue(kind, Number(value))}
          />
        </Bar>
      </BarChart>
    </ChartContainer>
  );
}
