import { cn } from "cn";
import { changeTone, type Card } from "@/modules/admin/reports/document";
import { formatChange, formatValue } from "@/modules/admin/reports/format";

export function ReportCard({ card }: { card: Card }) {
  return (
    <div className="flex min-w-0 flex-col gap-1.5 rounded-sm border border-hairline bg-surface-raised p-4">
      <span className="text-xs tracking-wide text-ink-muted uppercase">{card.label}</span>
      {card.type === "stat" ? (
        <>
          <span className="font-mono text-2xl text-ink">{card.value}</span>
          {card.hint && <span className="text-xs text-ink-muted">{card.hint}</span>}
        </>
      ) : (
        <>
          <span className="font-mono text-2xl text-ink">{formatValue(card.kind, card.metric.value)}</span>
          <span className="flex flex-wrap items-center gap-x-2 gap-y-0.5 text-xs">
            <span
              className={cn(
                "font-medium",
                changeTone(card) === "good" && "text-moss",
                changeTone(card) === "bad" && "text-brick",
                changeTone(card) === "neutral" && "text-ink-muted",
              )}
            >
              {formatChange(card.metric.change)}
            </span>
            <span className="text-ink-muted">antes {formatValue(card.kind, card.metric.previous)}</span>
          </span>
        </>
      )}
    </div>
  );
}
