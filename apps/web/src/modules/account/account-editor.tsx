"use client";

import { useState, useTransition, type FormEvent } from "react";
import { AccountForm } from "@/modules/account/account-form";
import type { AccountActionResult } from "@/modules/account/account-validation";
import type { Account, AccountFieldErrors } from "@/modules/account/types";

export function AccountEditor({
  account,
  action,
  submitLabel,
  savedMessage,
  onSaved,
  onCancel,
}: {
  account: Account;
  action: (formData: FormData) => Promise<AccountActionResult>;
  submitLabel: string;
  savedMessage?: string;
  onSaved?: () => void;
  onCancel?: () => void;
}) {
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<AccountFieldErrors>({});
  const [status, setStatus] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    setError(null);
    setStatus(null);

    startTransition(async () => {
      const result = await action(formData);

      if (!result.ok) {
        setError(result.message);
        setFieldErrors(result.fieldErrors);
        return;
      }

      setFieldErrors({});
      setStatus(savedMessage ?? null);
      onSaved?.();
    });
  }

  return (
    <AccountForm
      account={account}
      pending={pending}
      error={error}
      fieldErrors={fieldErrors}
      status={status}
      submitLabel={submitLabel}
      pendingLabel="Guardando…"
      onSubmit={handleSubmit}
      onCancel={onCancel}
    />
  );
}
