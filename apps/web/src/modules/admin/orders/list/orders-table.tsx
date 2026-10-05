"use client";

import Link from "next/link";
import { formatCalendarDate, formatMoment } from "@/lib/format-date";
import { formatPrice } from "@/lib/format-price";
import { OrderStatusBadge } from "@/modules/orders/order-status-badge";
import { OrderQuickAction } from "@/modules/admin/orders/list/order-quick-action";
import { ACTION_LABELS } from "@/modules/admin/orders/order-actions";
import type { AdminOrderSummary } from "@/modules/admin/orders/types";
import { DataTable, type Column } from "@/modules/admin/table/data-table";

const NEXT_ACTION = {
  Paid: ACTION_LABELS.Prepare,
  Processing: ACTION_LABELS.Ship,
  Shipped: ACTION_LABELS.Deliver,
} as const;

const columns: Column<AdminOrderSummary>[] = [
  {
    id: "orderNumber",
    header: "Pedido",
    sortKey: "orderNumber",
    cell: ({ row }) => (
      <Link href={`/admin/orders/${row.original.id}`} className="font-medium text-ink hover:text-accent">
        #{row.original.orderNumber}
      </Link>
    ),
  },
  {
    id: "placedAt",
    header: "Fecha",
    sortKey: "placedAt",
    className: "whitespace-nowrap",
    cell: ({ row }) => <span className="text-ink-muted">{formatMoment(row.original.placedAt)}</span>,
  },
  {
    id: "customer",
    header: "Cliente",
    sortKey: "customer",
    cell: ({ row }) => (
      <Link href={`/admin/customers/${row.original.customer.id}`} className="flex flex-col hover:text-accent">
        <span className="text-ink">{row.original.customer.name}</span>
        <span className="text-xs text-ink-muted">{row.original.customer.email}</span>
      </Link>
    ),
  },
  {
    id: "status",
    header: "Estado",
    cell: ({ row }) => <OrderStatusBadge status={row.original.status} />,
  },
  {
    id: "delivery",
    header: "Entrega estimada",
    className: "whitespace-nowrap",
    cell: ({ row }) =>
      row.original.status === "Delivered" || row.original.status === "Cancelled" ? (
        <span className="text-ink-muted">—</span>
      ) : (
        <span className="text-ink-muted">{formatCalendarDate(row.original.estimatedDeliveryDate)}</span>
      ),
  },
  {
    id: "items",
    header: "Artículos",
    className: "text-right",
    cell: ({ row }) => <span className="font-mono">{row.original.itemCount}</span>,
  },
  {
    id: "total",
    header: "Total",
    sortKey: "total",
    className: "text-right",
    cell: ({ row }) => <span className="font-mono text-ink">{formatPrice(row.original.total)}</span>,
  },
  {
    id: "next",
    header: () => <span className="sr-only">Siguiente paso</span>,
    className: "text-right",
    cell: ({ row }) => {
      const status = row.original.status;
      const next = status in NEXT_ACTION ? NEXT_ACTION[status as keyof typeof NEXT_ACTION] : null;
      return next ? <OrderQuickAction orderId={row.original.id} label={next.label} path={next.path} /> : null;
    },
  },
];

export function OrdersTable({ orders }: { orders: AdminOrderSummary[] }) {
  return (
    <DataTable
      columns={columns}
      data={orders}
      defaultSort="-placedAt"
      emptyMessage="No hay pedidos con estos filtros."
    />
  );
}
