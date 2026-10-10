import { cn } from "cn";

export type ChartKeyItem = { label: string; color: string; dashed?: boolean };

export function ChartKey({ items, className }: { items: ChartKeyItem[]; className?: string }) {
  return (
    <ul className={cn("flex flex-wrap gap-x-4 gap-y-1 text-xs text-ink-muted", className)}>
      {items.map((item) => (
        <li key={item.label} className="flex items-center gap-1.5">
          {item.dashed ? (
            <span className="w-4 border-t-2 border-dashed" style={{ borderColor: item.color }} />
          ) : (
            <span className="size-2.5 shrink-0 rounded-[2px]" style={{ backgroundColor: item.color }} />
          )}
          {item.label}
        </li>
      ))}
    </ul>
  );
}
