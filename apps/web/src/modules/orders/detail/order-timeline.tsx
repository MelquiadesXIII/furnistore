import { cn } from "cn";
import { formatCalendarDate, formatMoment } from "@/lib/format-date";
import type { Order } from "@/modules/orders/types";

type Step = { label: string; at: string | null; pending: string; note?: string | null };

function stepsFor(order: Order): Step[] {
  const paid: Step = { label: "Pagado", at: order.paidAt ?? order.placedAt, pending: "" };

  if (order.status === "Cancelled") {
    return [
      paid,
      ...(order.processingAt
        ? [{ label: "En preparación", at: order.processingAt, pending: "" }]
        : []),
      { label: "Cancelado", at: order.cancelledAt, pending: "", note: order.cancelReason },
    ];
  }

  return [
    paid,
    { label: "En preparación", at: order.processingAt, pending: "Pendiente" },
    { label: "Enviado", at: order.shippedAt, pending: "Pendiente" },
    {
      label: "Entregado",
      at: order.deliveredAt,
      pending: `Estimado para el ${formatCalendarDate(order.estimatedDeliveryDate)}`,
    },
  ];
}

export function OrderTimeline({ order }: { order: Order }) {
  const steps = stepsFor(order);
  const cancelled = order.status === "Cancelled";

  return (
    <ol className="flex flex-col">
      {steps.map((step, index) => {
        const done = step.at !== null;
        const last = index === steps.length - 1;

        return (
          <li key={step.label} className="relative flex gap-3 pb-5 last:pb-0">
            {!last && (
              <span
                aria-hidden="true"
                className={cn(
                  "absolute top-4 left-[5px] h-full w-px",
                  steps[index + 1].at !== null ? "bg-accent" : "bg-hairline",
                )}
              />
            )}
            <span
              aria-hidden="true"
              className={cn(
                "relative mt-1 size-[11px] shrink-0 rounded-full border-2",
                done
                  ? cancelled && last
                    ? "border-brick bg-brick"
                    : "border-accent bg-accent"
                  : "border-hairline bg-surface-raised",
              )}
            />
            <div className="flex flex-col gap-0.5">
              <span className={cn("text-sm font-medium", done ? "text-ink" : "text-ink-muted")}>
                {step.label}
              </span>
              <span className="text-xs text-ink-muted">
                {step.at ? formatMoment(step.at) : step.pending}
              </span>
              {step.note && <span className="text-xs text-ink-muted">Motivo: {step.note}</span>}
            </div>
          </li>
        );
      })}
    </ol>
  );
}
