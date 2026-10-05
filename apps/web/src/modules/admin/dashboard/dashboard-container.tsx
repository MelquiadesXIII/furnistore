import { AlertTriangle, ClipboardCheck, Hammer, Truck } from "lucide-react";
import Link from "next/link";
import { Panel } from "@/components/panel";
import { Card, CardContent } from "@/components/ui/card";
import { getAuditEntries } from "@/modules/admin/audit/api";
import { AuditEntryItem } from "@/modules/admin/audit/audit-entry-item";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { getAdminOrderStats } from "@/modules/admin/orders/api";
import { getAdminProducts } from "@/modules/admin/products/api";
import { AdminPageHeader } from "@/modules/admin/shell/admin-page-header";

export const LOW_STOCK_THRESHOLD = 3;

export async function DashboardContainer() {
  const [stats, lowStock, activity] = await Promise.all([
    getAdminOrderStats(),
    getAdminProducts({
      page: 1,
      pageSize: 1,
      q: "",
      status: "Active",
      categoryId: null,
      maxStock: LOW_STOCK_THRESHOLD,
      sort: "stock",
    }),
    getAuditEntries({ page: 1, pageSize: 8 }),
  ]);

  const cards = [
    {
      label: "Por preparar",
      hint: "Pagados, el cliente aún puede cancelar",
      value: stats.ok ? stats.value.paid : null,
      href: "/admin/orders?status=Paid&sort=placedAt",
      icon: ClipboardCheck,
    },
    {
      label: "En preparación",
      hint: "Listos para enviar",
      value: stats.ok ? stats.value.processing : null,
      href: "/admin/orders?status=Processing&sort=placedAt",
      icon: Hammer,
    },
    {
      label: "En camino",
      hint: "Falta confirmar la entrega",
      value: stats.ok ? stats.value.shipped : null,
      href: "/admin/orders?status=Shipped&sort=placedAt",
      icon: Truck,
    },
    {
      label: "Stock bajo",
      hint: `Productos activos con ${LOW_STOCK_THRESHOLD} o menos`,
      value: lowStock.ok ? lowStock.value.total : null,
      href: `/admin/products?status=Active&maxStock=${LOW_STOCK_THRESHOLD}&sort=stock`,
      icon: AlertTriangle,
    },
  ];

  return (
    <div className="flex flex-col gap-8">
      <AdminPageHeader title="Resumen" description="Lo que necesita tu atención ahora." />

      <ul className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {cards.map(({ label, hint, value, href, icon: Icon }) => (
          <li key={label}>
            <Link href={href} className="group block rounded-sm focus-visible:outline-2 focus-visible:outline-accent">
              <Card className="rounded-sm transition-colors group-hover:ring-accent">
                <CardContent className="flex flex-col gap-2">
                  <span className="flex items-center gap-2 text-sm text-ink-muted">
                    <Icon className="size-4 text-accent" />
                    {label}
                  </span>
                  <span className="font-mono text-3xl text-ink">{value ?? "—"}</span>
                  <span className="text-xs text-ink-muted">{hint}</span>
                </CardContent>
              </Card>
            </Link>
          </li>
        ))}
      </ul>

      <section className="flex flex-col gap-3">
        <div className="flex items-center justify-between">
          <h2 className="font-display text-lg font-semibold text-ink">Actividad reciente</h2>
          <Link href="/admin/audit" className="text-sm text-accent hover:underline">
            Ver todo
          </Link>
        </div>
        {!activity.ok ? (
          <Panel>{adminErrorMessage(activity.error)}</Panel>
        ) : activity.value.items.length === 0 ? (
          <Panel>Todavía no hay actividad registrada.</Panel>
        ) : (
          <Card className="rounded-sm">
            <CardContent>
              <ul className="divide-y divide-hairline">
                {activity.value.items.map((entry) => (
                  <AuditEntryItem key={entry.id} entry={entry} showChanges={false} />
                ))}
              </ul>
            </CardContent>
          </Card>
        )}
      </section>
    </div>
  );
}
