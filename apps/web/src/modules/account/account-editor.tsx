"use client";

import { useState, useTransition, type FormEvent } from "react";
import { AccountForm } from "@/modules/account/account-form";
import { saveAccount } from "@/modules/account/actions";
import type { Account, AccountFieldErrors } from "@/modules/account/types";

export function AccountEditor({
  account,
  returnTo,
  submitLabel,
  savedMessage,
  onSaved,
  onCancel,
}: {
  account: Account;
  returnTo: string;
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
      const result = await saveAccount(formData, returnTo);

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
