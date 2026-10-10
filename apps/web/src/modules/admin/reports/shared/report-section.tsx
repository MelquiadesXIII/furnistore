import { cn } from "cn";
import type { ReactNode } from "react";

export function ReportSection({
  title,
  description,
  flush = false,
  children,
}: {
  title: string;
  description?: ReactNode;
  flush?: boolean;
  children: ReactNode;
}) {
  return (
    <section
      className={cn(
        "flex min-w-0 flex-col gap-4 rounded-sm border border-hairline bg-surface-raised py-5",
        !flush && "px-5",
      )}
    >
      <header className={cn("flex flex-col gap-1", flush && "px-5")}>
        <h2 className="font-display text-base font-semibold text-ink">{title}</h2>
        {description && <p className="text-xs text-ink-muted">{description}</p>}
      </header>
      {children}
    </section>
  );
}
