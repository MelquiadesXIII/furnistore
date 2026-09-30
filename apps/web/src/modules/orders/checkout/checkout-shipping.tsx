"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { AccountEditor } from "@/modules/account/account-editor";
import { ShippingDetails } from "@/modules/account/shipping-details";
import type { Account } from "@/modules/account/types";

export function CheckoutShipping({ account }: { account: Account }) {
  const [editing, setEditing] = useState(false);
  const { address, phone } = account;
  const complete = account.isProfileComplete && address !== null && phone !== null;

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-end justify-between gap-4">
        <div className="flex flex-col gap-1">
          <h2 className="font-display text-xl font-semibold text-ink">Datos de envío</h2>
          {!complete && (
            <p className="text-sm text-ink-muted">
              Necesitamos tu teléfono y dirección para entregarte el pedido.
            </p>
          )}
        </div>
        {complete && !editing && (
          <Button type="button" variant="outline" size="sm" onClick={() => setEditing(true)}>
            Cambiar
          </Button>
        )}
      </div>

      <Card className="rounded-sm">
        <CardContent>
          {complete && !editing ? (
            <ShippingDetails
              name={`${account.firstName} ${account.lastName}`}
              phone={phone}
              address={address}
            />
          ) : (
            <AccountEditor
              account={account}
              returnTo="/checkout"
              submitLabel={complete ? "Guardar dirección" : "Guardar y continuar"}
              onSaved={() => setEditing(false)}
              onCancel={complete ? () => setEditing(false) : undefined}
            />
          )}
        </CardContent>
      </Card>
    </section>
  );
}
