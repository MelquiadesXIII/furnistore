import Link from "next/link";
import type { ReactNode } from "react";
import { Card, CardContent } from "@/components/ui/card";
import { formatMoment } from "@/lib/format-date";
import { formatPrice } from "@/lib/format-price";
import { OrderStatusBadge } from "@/modules/orders/order-status-badge";
import { AuditEntryItem } from "@/modules/admin/audit/audit-entry-item";
import { updateCustomer } from "@/modules/admin/customers/actions";
import { CustomerBadges } from "@/modules/admin/customers/customer-badges";
import { AccountActions } from "@/modules/admin/customers/detail/account-actions";
import { CustomerProfileEditor } from "@/modules/admin/customers/detail/customer-profile-editor";
import type { AdminCustomer } from "@/modules/admin/customers/types";

function Section({ title, children }: { title: string; children: ReactNode }) {
  return (
    <section className="flex flex-col gap-3">
      <h2 className="font-display text-base font-semibold text-ink">{title}</h2>
      <Card className="rounded-sm">
        <CardContent>{children}</CardContent>
      </Card>
    </section>
  );
}

export function CustomerView({ customer, isSelf }: { customer: AdminCustomer; isSelf: boolean }) {
  const { account } = customer;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-2 border-b border-hairline pb-5">
        <Link href="/admin/customers" className="text-sm text-ink-muted transition-colors hover:text-accent">
          ← Clientes
        </Link>
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="font-display text-2xl font-semibold tracking-tight text-ink">
            {customer.firstName} {customer.lastName}
          </h1>
          <CustomerBadges
            isAdmin={account.isAdmin}
            isDisabled={account.isDisabled}
            isLockedOut={Boolean(account.lockedOutUntil)}
            emailConfirmed={account.emailConfirmed}
          />
        </div>
        <p className="text-sm text-ink-muted">{customer.email}</p>
      </div>

      <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_22rem]">
        <div className="flex flex-col gap-6">
          <Section title="Datos de envío">
            <CustomerProfileEditor
              customer={customer}
              action={updateCustomer.bind(null, customer.id, customer.version)}
            />
          </Section>

          <Section title="Pedidos recientes">
            {customer.recentOrders.length === 0 ? (
              <p className="text-sm text-ink-muted">Todavía no hizo pedidos.</p>
            ) : (
              <ul className="divide-y divide-hairline">
                {customer.recentOrders.map((order) => (
                  <li key={order.id} className="flex items-center justify-between gap-4 py-2 first:pt-0 last:pb-0">
                    <Link href={`/admin/orders/${order.id}`} className="flex flex-col hover:text-accent">
                      <span className="font-medium text-ink">#{order.orderNumber}</span>
                      <span className="text-xs text-ink-muted">{formatMoment(order.placedAt)}</span>
                    </Link>
                    <span className="flex items-center gap-3">
                      <OrderStatusBadge status={order.status} />
                      <span className="font-mono text-sm">{formatPrice(order.total)}</span>
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </Section>

          <Section title="Carrito actual">
            {customer.cart.length === 0 ? (
              <p className="text-sm text-ink-muted">El carrito está vacío.</p>
            ) : (
              <ul className="flex flex-col gap-2 text-sm">
                {customer.cart.map((line) => (
                  <li key={line.productId} className="flex justify-between gap-4">
                    <Link href={`/admin/products/${line.productId}`} className="hover:text-accent">
                      {line.quantity} × {line.productName}
                    </Link>
                    <span className="font-mono">{formatPrice(line.lineTotal)}</span>
                  </li>
                ))}
              </ul>
            )}
          </Section>
        </div>

        <div className="flex flex-col gap-6">
          <Section title="Cuenta">
            <dl className="mb-4 grid grid-cols-[auto_1fr] gap-x-4 gap-y-1 text-sm">
              <dt className="text-ink-muted">Pedidos</dt>
              <dd className="font-mono">{customer.orderCount}</dd>
              <dt className="text-ink-muted">Total comprado</dt>
              <dd className="font-mono">{formatPrice(customer.totalSpent)}</dd>
              <dt className="text-ink-muted">Correo</dt>
              <dd>{account.emailConfirmed ? "Confirmado" : "Sin confirmar"}</dd>
              {account.lockedOutUntil && (
                <>
                  <dt className="text-ink-muted">Bloqueada hasta</dt>
                  <dd>{formatMoment(account.lockedOutUntil)}</dd>
                </>
              )}
              {account.failedLoginCount > 0 && (
                <>
                  <dt className="text-ink-muted">Intentos fallidos</dt>
                  <dd className="font-mono">{account.failedLoginCount}</dd>
                </>
              )}
            </dl>
            <AccountActions customer={customer} isSelf={isSelf} />
          </Section>

          <Section title="Historial">
            {customer.history.length === 0 ? (
              <p className="text-sm text-ink-muted">Sin cambios registrados.</p>
            ) : (
              <ul className="divide-y divide-hairline">
                {customer.history.map((entry) => (
                  <AuditEntryItem key={entry.id} entry={entry} />
                ))}
              </ul>
            )}
          </Section>
        </div>
      </div>
    </div>
  );
}
