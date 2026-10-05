"use client";

import { cn } from "cn";
import { parseAsInteger, parseAsString, useQueryStates } from "nuqs";
import { ORDER_STATUS_LABELS } from "@/modules/orders/order-status";
import { useTableTransition } from "@/modules/admin/table/table-frame";
import type { AdminOrderStats } from "@/modules/admin/orders/types";
import { ORDER_STATUSES } from "@/modules/admin/orders/search-params";

const COUNT_KEYS = {
  Paid: "paid",
  Processing: "processing",
  Shipped: "shipped",
  Delivered: "delivered",
  Cancelled: "cancelled",
} as const;

export function StatusTabs({ stats }: { stats: AdminOrderStats | null }) {
  const startTransition = useTableTransition();
  const [{ status }, setParams] = useQueryStates(
    { status: parseAsString, page: parseAsInteger, sort: parseAsString },
    { shallow: false, clearOnDefault: true, startTransition },
  );

  const tabs = [
    { value: null, label: "Todos", count: null },
    ...ORDER_STATUSES.map((value) => ({
      value,
      label: ORDER_STATUS_LABELS[value],
      count: stats ? stats[COUNT_KEYS[value]] : null,
    })),
  ];

  return (
    <div role="tablist" aria-label="Estado del pedido" className="flex gap-1 overflow-x-auto border-b border-hairline">
      {tabs.map((tab) => {
        const active = (status ?? null) === tab.value;
        const queue = tab.value === "Paid" || tab.value === "Processing" || tab.value === "Shipped";

        return (
          <button
            key={tab.label}
            type="button"
            role="tab"
            aria-selected={active}
            onClick={() =>
              setParams({ status: tab.value, page: null, sort: queue ? "placedAt" : null })
            }
            className={cn(
              "-mb-px flex shrink-0 items-center gap-2 border-b-2 border-transparent px-3 py-2 text-sm text-ink-muted transition-colors hover:text-ink",
              active && "border-accent font-medium text-ink",
            )}
          >
            {tab.label}
            {tab.count !== null && (
              <span className="rounded-sm bg-surface px-1.5 font-mono text-xs">{tab.count}</span>
            )}
          </button>
        );
      })}
    </div>
  );
}
