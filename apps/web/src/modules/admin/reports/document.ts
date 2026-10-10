import type { ValueKind } from "@/modules/admin/reports/format";
import type { ReportTableData } from "@/modules/admin/reports/tables/table-spec";
import type { ReportMetric } from "@/modules/admin/reports/types";

export type Tone =
  | "chart-1"
  | "chart-2"
  | "chart-3"
  | "chart-4"
  | "chart-5"
  | "chart-6"
  | "chart-7"
  | "chart-8"
  | "muted";

export const SERIES_TONES: Tone[] = [
  "chart-1",
  "chart-2",
  "chart-3",
  "chart-4",
  "chart-5",
  "chart-6",
  "chart-7",
  "chart-8",
];

export function seriesTone(index: number): Tone {
  return SERIES_TONES[index % SERIES_TONES.length];
}

export type Card =
  | { type: "metric"; label: string; kind: ValueKind; metric: ReportMetric; goodWhen?: "up" | "down" }
  | { type: "stat"; label: string; value: string; hint?: string };

export function changeTone(card: Extract<Card, { type: "metric" }>): "good" | "bad" | "neutral" {
  const { change } = card.metric;
  if (change === null || change === 0) return "neutral";
  return (change > 0 ? "up" : "down") === (card.goodWhen ?? "up") ? "good" : "bad";
}

export type TrendPoint = { label: string; value: number; previous: number | null };

export type ChartSpec =
  | {
      kind: "trend";
      variant: "area" | "bar";
      valueKind: ValueKind;
      valueLabel: string;
      previousLabel: string;
      points: TrendPoint[];
    }
  | {
      kind: "bars";
      layout: "horizontal" | "vertical";
      valueKind: ValueKind;
      valueLabel: string;
      items: { label: string; value: number; tone: Tone }[];
    }
  | {
      kind: "donut";
      valueKind: ValueKind;
      items: { label: string; value: number; share: number; tone: Tone }[];
    }
  | {
      kind: "stacked";
      valueKind: ValueKind;
      series: { label: string; tone: Tone }[];
      points: { label: string; values: number[] }[];
    };

export type ChartBlock = {
  type: "chart";
  title: string;
  description?: string;
  chart: ChartSpec | null;
  empty?: string;
};

export type ReportBlock =
  | { type: "cards"; columns: 3 | 4; cards: Card[] }
  | ChartBlock
  | { type: "pair"; blocks: [ChartBlock, ChartBlock] }
  | { type: "table"; table: ReportTableData };

export function tablesOf(blocks: ReportBlock[]): ReportTableData[] {
  return blocks.flatMap((block) => (block.type === "table" ? [block.table] : []));
}
