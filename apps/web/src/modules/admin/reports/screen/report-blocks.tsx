import { cn } from "cn";
import { BarsChart } from "@/modules/admin/reports/charts/bars-chart";
import { DonutChart } from "@/modules/admin/reports/charts/donut-chart";
import { StackedChart } from "@/modules/admin/reports/charts/stacked-chart";
import { TrendChart } from "@/modules/admin/reports/charts/trend-chart";
import type { ChartBlock, ChartSpec, ReportBlock } from "@/modules/admin/reports/document";
import { ReportCard } from "@/modules/admin/reports/shared/metric-card";
import { ReportSection } from "@/modules/admin/reports/shared/report-section";
import { ReportTable } from "@/modules/admin/reports/tables/report-table";

function Chart({ spec }: { spec: ChartSpec }) {
  switch (spec.kind) {
    case "trend":
      return <TrendChart spec={spec} />;
    case "bars":
      return <BarsChart spec={spec} />;
    case "donut":
      return <DonutChart spec={spec} />;
    case "stacked":
      return <StackedChart spec={spec} />;
  }
}

function ChartSection({ block }: { block: ChartBlock }) {
  return (
    <ReportSection title={block.title} description={block.description}>
      {block.chart ? <Chart spec={block.chart} /> : <p className="text-sm text-ink-muted">{block.empty}</p>}
    </ReportSection>
  );
}

export function ReportBlocks({ blocks }: { blocks: ReportBlock[] }) {
  return (
    <div className="flex flex-col gap-4">
      {blocks.map((block, index) => {
        switch (block.type) {
          case "cards":
            return (
              <div
                key={index}
                className={cn(
                  "grid gap-4",
                  block.columns === 4 ? "sm:grid-cols-2 xl:grid-cols-4" : "sm:grid-cols-3",
                )}
              >
                {block.cards.map((card) => (
                  <ReportCard key={card.label} card={card} />
                ))}
              </div>
            );
          case "chart":
            return <ChartSection key={index} block={block} />;
          case "pair":
            return (
              <div key={index} className="grid gap-4 lg:grid-cols-2">
                {block.blocks.map((child) => (
                  <ChartSection key={child.title} block={child} />
                ))}
              </div>
            );
          case "table":
            return <ReportTable key={block.table.id} table={block.table} />;
        }
      })}
    </div>
  );
}
