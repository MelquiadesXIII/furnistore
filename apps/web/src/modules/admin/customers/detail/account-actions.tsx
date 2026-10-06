"use client";

import { useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { changeAccount } from "@/modules/admin/customers/actions";
import type { AccountAction } from "@/modules/admin/customers/api";
import type { AdminCustomer } from "@/modules/admin/customers/types";

const CONFIRMATIONS: Partial<Record<AccountAction, string>> = {
  disable: "La persona no podrá iniciar sesión y se cierran sus sesiones abiertas.",
  "revoke-admin": "Pierde el acceso al panel cuando vuelva a iniciar sesión.",
  "grant-admin": "Tendrá acceso completo al panel cuando vuelva a iniciar sesión.",
};

export function AccountActions({ customer, isSelf }: { customer: AdminCustomer; isSelf: boolean }) {
  const [confirming, setConfirming] = useState<AccountAction | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();
  const { account } = customer;

  const actions: { action: AccountAction; label: string; destructive?: boolean; hint?: string }[] = [];

  if (!account.isDisabled && (account.lockedOutUntil || account.failedLoginCount > 0)) {
    actions.push({ action: "unlock", label: "Desbloquear inicio de sesión" });
  }

  if (account.isAdmin) {
    actions.push({
      action: "revoke-admin",
      label: "Quitar rol de administrador",
      destructive: true,
      hint: isSelf ? "No puedes quitarte el rol a ti mismo." : undefined,
    });
  } else if (!account.isDisabled) {
    actions.push({
      action: "grant-admin",
      label: "Hacer administrador",
      hint: account.emailConfirmed ? undefined : "Primero tiene que confirmar su correo.",
    });
  }

  if (account.isDisabled) {
    actions.push({ action: "enable", label: "Reactivar cuenta" });
  } else {
    actions.push({
      action: "disable",
      label: "Desactivar cuenta",
      destructive: true,
      hint: isSelf
        ? "No puedes desactivar tu propia cuenta."
        : account.isAdmin
          ? "Quítale el rol de administrador antes de desactivarla."
          : undefined,
    });
  }

  function run(action: AccountAction) {
    setError(null);
    startTransition(async () => {
      const result = await changeAccount(customer.id, action);
      setConfirming(null);
      if (!result.ok) setError(result.message);
    });
  }

  return (
    <div className="flex flex-col gap-2">
      {actions.map(({ action, label, destructive, hint }) =>
        confirming === action ? (
          <div key={action} className="flex flex-col gap-2 rounded-sm border border-brick/40 p-3">
            <p className="text-sm text-ink">{CONFIRMATIONS[action]}</p>
            <div className="flex gap-2">
              <Button
                type="button"
                size="sm"
                variant={destructive ? "destructive" : "default"}
                disabled={pending}
                onClick={() => run(action)}
              >
                {pending ? "Guardando…" : "Confirmar"}
              </Button>
              <Button type="button" size="sm" variant="ghost" disabled={pending} onClick={() => setConfirming(null)}>
                Cancelar
              </Button>
            </div>
          </div>
        ) : (
          <div key={action} className="flex flex-col gap-1">
            <Button
              type="button"
              variant="outline"
              className={destructive ? "text-brick hover:text-brick" : undefined}
              disabled={pending || Boolean(hint)}
              onClick={() => (CONFIRMATIONS[action] ? setConfirming(action) : run(action))}
            >
              {label}
            </Button>
            {hint && <span className="text-xs text-ink-muted">{hint}</span>}
          </div>
        ),
      )}
      <p role="alert" className="text-sm text-brick empty:hidden">
        {error}
      </p>
    </div>
  );
}
