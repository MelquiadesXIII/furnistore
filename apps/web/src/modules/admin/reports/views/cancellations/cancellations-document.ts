import type { ApiSchemas } from "@/lib/api/contract";
import type { ReportBlock, Tone } from "@/modules/admin/reports/document";
import type { CancellationsReport } from "@/modules/admin/reports/types";
import {
  ACTOR_LABELS,
  cancellationsTables,
  stageLabel,
} from "@/modules/admin/reports/views/cancellations/cancellations-tables";

const ACTOR_TONES: Record<ApiSchemas["CancellationActor"], Tone> = {
  Customer: "chart-1",
  Admin: "chart-3",
  Unknown: "chart-8",
};

const STAGE_TONES: Tone[] = ["chart-1", "chart-4", "chart-8"];

export function cancellationsDocument(report: CancellationsReport): ReportBlock[] {
  const blocks: ReportBlock[] = [
    {
      type: "cards",
      columns: 3,
      cards: [
        { type: "metric", label: "Pedidos cancelados", kind: "count", metric: report.cancelled, goodWhen: "down" },
        { type: "metric", label: "Tasa de cancelación", kind: "percent", metric: report.rate, goodWhen: "down" },
        { type: "metric", label: "Ingresos perdidos", kind: "money", metric: report.lostRevenue, goodWhen: "down" },
      ],
    },
  ];

  if (report.cancelled.value > 0) {
    blocks.push({
      type: "pair",
      blocks: [
        {
          type: "chart",
          title: "Quién cancela",
          description: "Según el registro de auditoría de cada pedido.",
          chart: {
            kind: "donut",
            valueKind: "count",
            items: report.byActor.map((row) => ({
              label: ACTOR_LABELS[row.actor],
              value: row.orders,
              share: row.share,
              tone: ACTOR_TONES[row.actor],
            })),
          },
        },
        {
          type: "chart",
          title: "En qué etapa",
          description: "Estado del pedido justo antes de cancelarse.",
          chart: {
            kind: "donut",
            valueKind: "count",
            items: report.byStage.map((row, index) => ({
              label: stageLabel(row.stage),
              value: row.orders,
              share: row.share,
              tone: STAGE_TONES[index % STAGE_TONES.length],
            })),
          },
        },
      ],
    });
  }

  return [...blocks, ...cancellationsTables(report).map((table) => ({ type: "table" as const, table }))];
}
