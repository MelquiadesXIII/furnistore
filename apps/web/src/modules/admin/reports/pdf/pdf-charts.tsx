import { Circle, Line, Path, Rect, Svg, Text, View } from "@react-pdf/renderer";
import { Fragment } from "react";
import type { ChartSpec } from "@/modules/admin/reports/document";
import { formatAxisValue, formatValue, type ValueKind } from "@/modules/admin/reports/format";
import { PDF_COLORS, pdfTone } from "@/modules/admin/reports/pdf/theme";

const AXIS_WIDTH = 46;
const AXIS_FONT = 6.5;
const LABEL_FONT = 7;
const MAX_X_LABELS = 8;

function niceMax(value: number): number {
  if (value <= 0) return 1;
  const magnitude = 10 ** Math.floor(Math.log10(value));
  const step = [1, 2, 2.5, 5, 10].find((candidate) => value / magnitude <= candidate) ?? 10;
  return step * magnitude;
}

function truncate(text: string, limit: number): string {
  return text.length > limit ? `${text.slice(0, limit - 1)}…` : text;
}

function Label({
  x,
  y,
  width,
  align = "left",
  size = LABEL_FONT,
  color = PDF_COLORS.muted,
  children,
}: {
  x: number;
  y: number;
  width: number;
  align?: "left" | "center" | "right";
  size?: number;
  color?: string;
  children: string;
}) {
  return (
    <Text
      style={{ position: "absolute", left: x, top: y, width, fontSize: size, color, textAlign: align }}
    >
      {children}
    </Text>
  );
}

function YAxis({ max, kind, top, plotHeight }: { max: number; kind: ValueKind; top: number; plotHeight: number }) {
  return (
    <>
      {[0, 0.25, 0.5, 0.75, 1].map((fraction) => (
        <Label
          key={fraction}
          x={0}
          y={top + plotHeight * (1 - fraction) - AXIS_FONT / 2 - 1}
          width={AXIS_WIDTH - 6}
          align="right"
          size={AXIS_FONT}
        >
          {formatAxisValue(kind, max * fraction)}
        </Label>
      ))}
    </>
  );
}

function Grid({ width, top, plotHeight }: { width: number; top: number; plotHeight: number }) {
  return (
    <>
      {[0, 0.25, 0.5, 0.75, 1].map((fraction) => (
        <Line
          key={fraction}
          x1={AXIS_WIDTH}
          x2={width}
          y1={top + plotHeight * (1 - fraction)}
          y2={top + plotHeight * (1 - fraction)}
          stroke={PDF_COLORS.hairline}
          strokeWidth={0.5}
        />
      ))}
    </>
  );
}

function xLabelLeft(center: number, width: number): number {
  return Math.min(Math.max(center - 30, AXIS_WIDTH - 10), width - 60);
}

function xLabelStep(count: number): number {
  return Math.max(1, Math.ceil(count / MAX_X_LABELS));
}

function Key({ items }: { items: { label: string; color: string; dashed?: boolean }[] }) {
  return (
    <View style={{ flexDirection: "row", flexWrap: "wrap", gap: 10, marginTop: 6 }}>
      {items.map((item) => (
        <View key={item.label} style={{ flexDirection: "row", alignItems: "center", gap: 4 }}>
          {item.dashed ? (
            <Svg width={12} height={4}>
              <Line x1={0} x2={12} y1={2} y2={2} stroke={item.color} strokeWidth={1.2} strokeDasharray="2 2" />
            </Svg>
          ) : (
            <View style={{ width: 6, height: 6, backgroundColor: item.color }} />
          )}
          <Text style={{ fontSize: LABEL_FONT, color: PDF_COLORS.muted }}>{item.label}</Text>
        </View>
      ))}
    </View>
  );
}

function TrendPdf({ spec, width }: { spec: Extract<ChartSpec, { kind: "trend" }>; width: number }) {
  const height = spec.variant === "area" ? 170 : 130;
  const top = 6;
  const plotHeight = height - top - 16;
  const plotWidth = width - AXIS_WIDTH;
  const count = spec.points.length;
  const max = niceMax(Math.max(0, ...spec.points.flatMap((point) => [point.value, point.previous ?? 0])));
  const y = (value: number) => top + plotHeight * (1 - value / max);
  const band = plotWidth / Math.max(count, 1);
  const x = (index: number) =>
    spec.variant === "bar" || count === 1
      ? AXIS_WIDTH + band * (index + 0.5)
      : AXIS_WIDTH + (plotWidth * index) / (count - 1);
  const current = pdfTone("chart-1");
  const previous = pdfTone("muted");

  const line = spec.points.map((point, index) => `${index === 0 ? "M" : "L"}${x(index)},${y(point.value)}`).join(" ");
  const area = `${line} L${x(count - 1)},${y(0)} L${x(0)},${y(0)} Z`;
  const previousPoints = spec.points
    .map((point, index) => (point.previous === null ? null : `${x(index)},${y(point.previous)}`))
    .filter((point): point is string => point !== null);
  const step = xLabelStep(count);

  return (
    <View>
      <View style={{ position: "relative", width, height }}>
        <Svg width={width} height={height}>
          <Grid width={width} top={top} plotHeight={plotHeight} />
          {spec.variant === "area" ? (
            <>
              <Path d={area} fill={current} fillOpacity={0.18} />
              <Path d={line} stroke={current} strokeWidth={1.5} fill="none" />
            </>
          ) : (
            spec.points.map((point, index) => {
              const barWidth = Math.min(band * 0.7, 24);
              return (
                <Rect
                  key={index}
                  x={x(index) - barWidth / 2}
                  y={y(point.value)}
                  width={barWidth}
                  height={y(0) - y(point.value)}
                  fill={current}
                />
              );
            })
          )}
          {previousPoints.length > 1 && (
            <Path
              d={`M${previousPoints.join(" L")}`}
              stroke={previous}
              strokeWidth={1}
              strokeDasharray="3 3"
              fill="none"
            />
          )}
        </Svg>
        <YAxis max={max} kind={spec.valueKind} top={top} plotHeight={plotHeight} />
        {spec.points.map((point, index) =>
          index % step === 0 ? (
            <Label key={index} x={xLabelLeft(x(index), width)} y={height - 11} width={60} align="center" size={AXIS_FONT}>
              {point.label}
            </Label>
          ) : null,
        )}
      </View>
      <Key
        items={[
          { label: spec.valueLabel, color: current },
          { label: spec.previousLabel, color: previous, dashed: true },
        ]}
      />
    </View>
  );
}

function BarsPdf({ spec, width }: { spec: Extract<ChartSpec, { kind: "bars" }>; width: number }) {
  const max = niceMax(Math.max(0, ...spec.items.map((item) => item.value)));

  if (spec.layout === "horizontal") {
    const labelWidth = Math.min(130, width * 0.36);
    const valueWidth = 62;
    const barSpace = width - labelWidth - valueWidth - 6;
    const row = 16;
    const height = spec.items.length * row;

    return (
      <View style={{ position: "relative", width, height }}>
        <Svg width={width} height={height}>
          {spec.items.map((item, index) => (
            <Rect
              key={index}
              x={labelWidth + 6}
              y={index * row + 3}
              width={Math.max(1, (barSpace * item.value) / max)}
              height={row - 6}
              fill={pdfTone(item.tone)}
            />
          ))}
        </Svg>
        {spec.items.map((item, index) => (
          <Fragment key={index}>
            <Label x={0} y={index * row + 4} width={labelWidth} align="right" color={PDF_COLORS.ink}>
              {truncate(item.label, Math.floor(labelWidth / 3.7))}
            </Label>
            <Label
              x={labelWidth + 10 + (barSpace * item.value) / max}
              y={index * row + 4}
              width={valueWidth}
              color={PDF_COLORS.ink}
            >
              {formatValue(spec.valueKind, item.value)}
            </Label>
          </Fragment>
        ))}
      </View>
    );
  }

  const height = 140;
  const top = 12;
  const plotHeight = height - top - 16;
  const plotWidth = width - AXIS_WIDTH;
  const band = plotWidth / Math.max(spec.items.length, 1);
  const barWidth = Math.min(band * 0.6, 40);
  const y = (value: number) => top + plotHeight * (1 - value / max);

  return (
    <View style={{ position: "relative", width, height }}>
      <Svg width={width} height={height}>
        <Grid width={width} top={top} plotHeight={plotHeight} />
        {spec.items.map((item, index) => (
          <Rect
            key={index}
            x={AXIS_WIDTH + band * (index + 0.5) - barWidth / 2}
            y={y(item.value)}
            width={barWidth}
            height={y(0) - y(item.value)}
            fill={pdfTone(item.tone)}
          />
        ))}
      </Svg>
      <YAxis max={max} kind={spec.valueKind} top={top} plotHeight={plotHeight} />
      {spec.items.map((item, index) => (
        <Fragment key={index}>
          <Label
            x={AXIS_WIDTH + band * index}
            y={y(item.value) - 10}
            width={band}
            align="center"
            color={PDF_COLORS.ink}
          >
            {formatValue(spec.valueKind, item.value)}
          </Label>
          <Label x={AXIS_WIDTH + band * index} y={height - 11} width={band} align="center" size={AXIS_FONT}>
            {truncate(item.label, 18)}
          </Label>
        </Fragment>
      ))}
    </View>
  );
}

function arc(cx: number, cy: number, outer: number, inner: number, start: number, end: number): string {
  const point = (radius: number, angle: number) =>
    `${cx + radius * Math.sin(angle)},${cy - radius * Math.cos(angle)}`;
  const large = end - start > Math.PI ? 1 : 0;
  return [
    `M${point(outer, start)}`,
    `A${outer},${outer} 0 ${large} 1 ${point(outer, end)}`,
    `L${point(inner, end)}`,
    `A${inner},${inner} 0 ${large} 0 ${point(inner, start)}`,
    "Z",
  ].join(" ");
}

function DonutPdf({ spec, width }: { spec: Extract<ChartSpec, { kind: "donut" }>; width: number }) {
  const size = 92;
  const outer = size / 2;
  const inner = outer * 0.58;
  const total = spec.items.reduce((sum, item) => sum + item.value, 0);
  let angle = 0;

  return (
    <View style={{ flexDirection: "row", alignItems: "center", gap: 12, width }}>
      <Svg width={size} height={size}>
        {spec.items.length === 1 || total === 0 ? (
          <Circle
            cx={outer}
            cy={outer}
            r={(outer + inner) / 2}
            stroke={pdfTone(spec.items[0]?.tone ?? "muted")}
            strokeWidth={outer - inner}
            fill="none"
          />
        ) : (
          spec.items.map((item, index) => {
            const start = angle;
            angle += (item.value / total) * Math.PI * 2;
            return item.value > 0 ? (
              <Path
                key={index}
                d={arc(outer, outer, outer, inner, start, angle)}
                fill={pdfTone(item.tone)}
                stroke={PDF_COLORS.raised}
                strokeWidth={0.8}
              />
            ) : null;
          })
        )}
      </Svg>
      <View style={{ flex: 1, gap: 4 }}>
        {spec.items.map((item) => (
          <View key={item.label} style={{ flexDirection: "row", alignItems: "center", gap: 4 }}>
            <View style={{ width: 6, height: 6, backgroundColor: pdfTone(item.tone) }} />
            <Text style={{ flex: 1, fontSize: LABEL_FONT, color: PDF_COLORS.ink }}>{item.label}</Text>
            <Text style={{ width: 56, fontSize: LABEL_FONT, color: PDF_COLORS.muted, textAlign: "right" }}>
              {formatValue(spec.valueKind, item.value)}
            </Text>
            <Text style={{ width: 34, fontSize: LABEL_FONT, color: PDF_COLORS.ink, textAlign: "right" }}>
              {formatValue("percent", item.share)}
            </Text>
          </View>
        ))}
      </View>
    </View>
  );
}

function StackedPdf({ spec, width }: { spec: Extract<ChartSpec, { kind: "stacked" }>; width: number }) {
  const height = 150;
  const top = 6;
  const plotHeight = height - top - 16;
  const plotWidth = width - AXIS_WIDTH;
  const totals = spec.points.map((point) => point.values.reduce((sum, value) => sum + value, 0));
  const max = niceMax(Math.max(0, ...totals));
  const band = plotWidth / Math.max(spec.points.length, 1);
  const barWidth = Math.min(band * 0.7, 24);
  const scale = (value: number) => (plotHeight * value) / max;
  const step = xLabelStep(spec.points.length);

  return (
    <View>
      <View style={{ position: "relative", width, height }}>
        <Svg width={width} height={height}>
          <Grid width={width} top={top} plotHeight={plotHeight} />
          {spec.points.map((point, index) => {
            let base = top + plotHeight;
            return point.values.map((value, series) => {
              const barHeight = scale(value);
              base -= barHeight;
              return value > 0 ? (
                <Rect
                  key={`${index}-${series}`}
                  x={AXIS_WIDTH + band * (index + 0.5) - barWidth / 2}
                  y={base}
                  width={barWidth}
                  height={barHeight}
                  fill={pdfTone(spec.series[series].tone)}
                />
              ) : null;
            });
          })}
        </Svg>
        <YAxis max={max} kind={spec.valueKind} top={top} plotHeight={plotHeight} />
        {spec.points.map((point, index) =>
          index % step === 0 ? (
            <Label
              key={index}
              x={xLabelLeft(AXIS_WIDTH + band * (index + 0.5), width)}
              y={height - 11}
              width={60}
              align="center"
              size={AXIS_FONT}
            >
              {point.label}
            </Label>
          ) : null,
        )}
      </View>
      <Key items={spec.series.map((entry) => ({ label: entry.label, color: pdfTone(entry.tone) }))} />
    </View>
  );
}

export function PdfChart({ spec, width }: { spec: ChartSpec; width: number }) {
  switch (spec.kind) {
    case "trend":
      return <TrendPdf spec={spec} width={width} />;
    case "bars":
      return <BarsPdf spec={spec} width={width} />;
    case "donut":
      return <DonutPdf spec={spec} width={width} />;
    case "stacked":
      return <StackedPdf spec={spec} width={width} />;
  }
}
