import { Circle, Line, Path, Rect, Svg, Text, View } from "@react-pdf/renderer";
import { Fragment } from "react";
import { formatAxisValue, formatValue, type ValueKind } from "@/modules/admin/reports/format";
import { PDF_COLORS } from "@/modules/admin/reports/pdf/theme";

const AXIS_WIDTH = 46;
const FONT = 7;

function niceMax(value: number): number {
  if (value <= 0) return 1;
  const magnitude = 10 ** Math.floor(Math.log10(value));
  const step = [1, 2, 2.5, 5, 10].find((candidate) => value / magnitude <= candidate) ?? 10;
  return step * magnitude;
}

function Label({
  x,
  y,
  width,
  align = "left",
  children,
}: {
  x: number;
  y: number;
  width: number;
  align?: "left" | "center" | "right";
  children: string;
}) {
  return (
    <Text
      style={{ position: "absolute", left: x, top: y, width, fontSize: FONT, color: PDF_COLORS.muted, textAlign: align }}
    >
      {children}
    </Text>
  );
}

function GridLines({ top, height, width }: { top: number; height: number; width: number }) {
  return (
    <>
      {[0, 0.5, 1].map((fraction) => (
        <Line
          key={fraction}
          x1={AXIS_WIDTH}
          x2={width}
          y1={top + height * (1 - fraction)}
          y2={top + height * (1 - fraction)}
          stroke={PDF_COLORS.hairline}
          strokeWidth={0.5}
        />
      ))}
    </>
  );
}

function AxisLabels({ max, kind, top, height }: { max: number; kind: ValueKind; top: number; height: number }) {
  return (
    <>
      {[0, 0.5, 1].map((fraction) => (
        <Label key={fraction} x={0} y={top + height * (1 - fraction) - 4} width={AXIS_WIDTH - 6} align="right">
          {formatAxisValue(kind, max * fraction)}
        </Label>
      ))}
    </>
  );
}

export function PdfAreaChart({
  points,
  kind,
  width,
}: {
  points: { label: string; value: number }[];
  kind: ValueKind;
  width: number;
}) {
  const height = 160;
  const top = 6;
  const plot = height - top - 16;
  const max = niceMax(Math.max(0, ...points.map((point) => point.value)));
  const step = Math.max(1, Math.ceil(points.length / 8));
  const x = (index: number) =>
    points.length === 1
      ? AXIS_WIDTH + (width - AXIS_WIDTH) / 2
      : AXIS_WIDTH + ((width - AXIS_WIDTH) * index) / (points.length - 1);
  const y = (value: number) => top + plot * (1 - value / max);

  const line = points.map((point, index) => `${index === 0 ? "M" : "L"}${x(index)},${y(point.value)}`).join(" ");
  const area = `${line} L${x(points.length - 1)},${y(0)} L${x(0)},${y(0)} Z`;

  return (
    <View style={{ position: "relative", width, height }}>
      <Svg width={width} height={height}>
        <GridLines top={top} height={plot} width={width} />
        <Path d={area} fill={PDF_COLORS.accent} fillOpacity={0.2} />
        <Path d={line} stroke={PDF_COLORS.accent} strokeWidth={1.5} fill="none" />
      </Svg>
      <AxisLabels max={max} kind={kind} top={top} height={plot} />
      {points.map((point, index) =>
        index % step === 0 ? (
          <Label
            key={index}
            x={Math.min(Math.max(x(index) - 30, AXIS_WIDTH - 10), width - 60)}
            y={height - 11}
            width={60}
            align="center"
          >
            {point.label}
          </Label>
        ) : null,
      )}
    </View>
  );
}

export function PdfBarsChart({
  items,
  kind,
  width,
  horizontal = false,
}: {
  items: { label: string; value: number; color?: string }[];
  kind: ValueKind;
  width: number;
  horizontal?: boolean;
}) {
  const max = niceMax(Math.max(0, ...items.map((item) => item.value)));

  if (horizontal) {
    const labelWidth = 140;
    const barSpace = width - labelWidth - 70;
    const row = 16;

    return (
      <View style={{ position: "relative", width, height: items.length * row }}>
        <Svg width={width} height={items.length * row}>
          {items.map((item, index) => (
            <Rect
              key={index}
              x={labelWidth + 6}
              y={index * row + 3}
              width={Math.max(1, (barSpace * item.value) / max)}
              height={row - 6}
              fill={item.color ?? PDF_COLORS.accent}
            />
          ))}
        </Svg>
        {items.map((item, index) => (
          <Fragment key={index}>
            <Label x={0} y={index * row + 4} width={labelWidth} align="right">
              {item.label.length > 36 ? `${item.label.slice(0, 35)}…` : item.label}
            </Label>
            <Label x={labelWidth + 10 + (barSpace * item.value) / max} y={index * row + 4} width={64}>
              {formatValue(kind, item.value)}
            </Label>
          </Fragment>
        ))}
      </View>
    );
  }

  const height = 150;
  const top = 12;
  const plot = height - top - 16;
  const band = (width - AXIS_WIDTH) / Math.max(items.length, 1);
  const barWidth = Math.min(band * 0.6, 40);
  const y = (value: number) => top + plot * (1 - value / max);

  return (
    <View style={{ position: "relative", width, height }}>
      <Svg width={width} height={height}>
        <GridLines top={top} height={plot} width={width} />
        {items.map((item, index) => (
          <Rect
            key={index}
            x={AXIS_WIDTH + band * (index + 0.5) - barWidth / 2}
            y={y(item.value)}
            width={barWidth}
            height={y(0) - y(item.value)}
            fill={item.color ?? PDF_COLORS.accent}
          />
        ))}
      </Svg>
      <AxisLabels max={max} kind={kind} top={top} height={plot} />
      {items.map((item, index) => (
        <Fragment key={index}>
          <Label x={AXIS_WIDTH + band * index} y={y(item.value) - 10} width={band} align="center">
            {formatValue(kind, item.value)}
          </Label>
          <Label x={AXIS_WIDTH + band * index} y={height - 11} width={band} align="center">
            {item.label}
          </Label>
        </Fragment>
      ))}
    </View>
  );
}

function slice(center: number, outer: number, inner: number, start: number, end: number): string {
  const point = (radius: number, angle: number) =>
    `${center + radius * Math.sin(angle)},${center - radius * Math.cos(angle)}`;
  const large = end - start > Math.PI ? 1 : 0;
  return `M${point(outer, start)} A${outer},${outer} 0 ${large} 1 ${point(outer, end)} L${point(inner, end)} A${inner},${inner} 0 ${large} 0 ${point(inner, start)} Z`;
}

export function PdfDonutChart({ items }: { items: { label: string; value: number; share: number; color: string }[] }) {
  const size = 100;
  const outer = size / 2;
  const inner = outer * 0.55;
  const total = items.reduce((sum, item) => sum + item.value, 0);
  let angle = 0;

  return (
    <View style={{ flexDirection: "row", alignItems: "center", gap: 16 }}>
      <Svg width={size} height={size}>
        {items.length === 1 ? (
          <Circle
            cx={outer}
            cy={outer}
            r={(outer + inner) / 2}
            stroke={items[0].color}
            strokeWidth={outer - inner}
            fill="none"
          />
        ) : (
          items.map((item, index) => {
            const start = angle;
            angle += (item.value / total) * Math.PI * 2;
            return <Path key={index} d={slice(outer, outer, inner, start, angle)} fill={item.color} />;
          })
        )}
      </Svg>
      <View style={{ gap: 4 }}>
        {items.map((item) => (
          <View key={item.label} style={{ flexDirection: "row", alignItems: "center", gap: 4 }}>
            <View style={{ width: 7, height: 7, backgroundColor: item.color }} />
            <Text style={{ fontSize: 8 }}>
              {item.label}: {formatValue("count", item.value)} ({formatValue("percent", item.share)})
            </Text>
          </View>
        ))}
      </View>
    </View>
  );
}
