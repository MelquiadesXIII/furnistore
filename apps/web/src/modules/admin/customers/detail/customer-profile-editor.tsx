"use client";

import { useState } from "react";
import type { AccountActionResult } from "@/modules/account/account-validation";
import { AccountEditor } from "@/modules/account/account-editor";
import type { AdminCustomer } from "@/modules/admin/customers/types";

export function CustomerProfileEditor({
  customer,
  action,
}: {
  customer: AdminCustomer;
  action: (formData: FormData) => Promise<AccountActionResult>;
}) {
  const [saved, setSaved] = useState(false);

  return (
    <div className="flex flex-col gap-4">
      {saved && (
        <p role="status" className="rounded-sm border border-accent/40 bg-accent/10 px-3 py-2 text-sm text-ink">
          Datos guardados.
        </p>
      )}
      <AccountEditor
        key={customer.version}
        account={customer}
        action={async (formData) => {
          const result = await action(formData);
          setSaved(result.ok);
          return result;
        }}
        submitLabel="Guardar datos"
      />
    </div>
  );
}
