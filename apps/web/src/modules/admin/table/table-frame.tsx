"use client";

import { createContext, useContext, useTransition, type ReactNode, type TransitionStartFunction } from "react";

const TableTransition = createContext<TransitionStartFunction | null>(null);

export function TableFrame({ children }: { children: ReactNode }) {
  const [pending, startTransition] = useTransition();

  return (
    <TableTransition.Provider value={startTransition}>
      <div
        aria-busy={pending}
        className="flex flex-col gap-4 transition-opacity aria-busy:opacity-60"
      >
        {children}
      </div>
    </TableTransition.Provider>
  );
}

export function useTableTransition(): TransitionStartFunction | undefined {
  return useContext(TableTransition) ?? undefined;
}
