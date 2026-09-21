import type { ReactNode } from "react";

export function Panel({ children }: { children: ReactNode }) {
  return (
    <div className="flex flex-col items-center gap-3 border border-dashed border-hairline py-20 text-center">
      <p className="text-ink-muted">{children}</p>
    </div>
  );
}
