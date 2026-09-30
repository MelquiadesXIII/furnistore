import { cn } from "cn";
import type { FormEvent, ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import type { Account, AccountField, AccountFieldErrors } from "@/modules/account/types";

function Field({
  id,
  label,
  error,
  hint,
  className,
  children,
}: {
  id: AccountField;
  label: string;
  error?: string;
  hint?: string;
  className?: string;
  children: ReactNode;
}) {
  return (
    <div className={cn("flex flex-col gap-1.5", className)}>
      <Label htmlFor={id} className="text-ink-muted">
        {label}
      </Label>
      {children}
      {error ? (
        <p id={`${id}-error`} className="text-xs text-brick">
          {error}
        </p>
      ) : (
        hint && <p className="text-xs text-ink-muted">{hint}</p>
      )}
    </div>
  );
}

function fieldProps(id: AccountField, fieldErrors: AccountFieldErrors) {
  const error = fieldErrors[id];
  return {
    id,
    name: id,
    "aria-invalid": error ? true : undefined,
    "aria-describedby": error ? `${id}-error` : undefined,
    className: "bg-surface-raised",
  };
}

export function AccountForm({
  account,
  pending,
  error,
  fieldErrors,
  status,
  submitLabel,
  pendingLabel,
  onSubmit,
  onCancel,
}: {
  account: Account;
  pending: boolean;
  error: string | null;
  fieldErrors: AccountFieldErrors;
  status: string | null;
  submitLabel: string;
  pendingLabel: string;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onCancel?: () => void;
}) {
  return (
    <form onSubmit={onSubmit} noValidate className="flex w-full flex-col gap-5">
      <div className="grid gap-5 sm:grid-cols-2">
        <Field id="firstName" label="Nombre" error={fieldErrors.firstName}>
          <Input
            {...fieldProps("firstName", fieldErrors)}
            type="text"
            required
            minLength={2}
            maxLength={60}
            autoComplete="given-name"
            defaultValue={account.firstName}
          />
        </Field>

        <Field id="lastName" label="Apellidos" error={fieldErrors.lastName}>
          <Input
            {...fieldProps("lastName", fieldErrors)}
            type="text"
            required
            minLength={2}
            maxLength={60}
            autoComplete="family-name"
            defaultValue={account.lastName}
          />
        </Field>
      </div>

      <Field
        id="phone"
        label="Teléfono"
        error={fieldErrors.phone}
        hint="De 7 a 15 dígitos. Puedes incluir el prefijo internacional con +."
      >
        <Input
          {...fieldProps("phone", fieldErrors)}
          type="tel"
          inputMode="tel"
          required
          maxLength={25}
          autoComplete="tel"
          defaultValue={account.phone ?? ""}
        />
      </Field>

      <Field
        id="street"
        label="Dirección"
        error={fieldErrors.street}
        hint="Calle, número y apartamento."
      >
        <Input
          {...fieldProps("street", fieldErrors)}
          type="text"
          required
          minLength={3}
          maxLength={200}
          autoComplete="street-address"
          defaultValue={account.address?.street ?? ""}
        />
      </Field>

      <div className="grid gap-5 sm:grid-cols-2">
        <Field id="city" label="Ciudad" error={fieldErrors.city}>
          <Input
            {...fieldProps("city", fieldErrors)}
            type="text"
            required
            minLength={2}
            maxLength={100}
            autoComplete="address-level2"
            defaultValue={account.address?.city ?? ""}
          />
        </Field>

        <Field id="province" label="Provincia" error={fieldErrors.province}>
          <Input
            {...fieldProps("province", fieldErrors)}
            type="text"
            required
            minLength={2}
            maxLength={100}
            autoComplete="address-level1"
            defaultValue={account.address?.province ?? ""}
          />
        </Field>
      </div>

      <Field
        id="deliveryNotes"
        label="Indicaciones para la entrega (opcional)"
        error={fieldErrors.deliveryNotes}
        hint="Por ejemplo: entre qué calles está, o a qué hora hay alguien en casa."
      >
        <Textarea
          {...fieldProps("deliveryNotes", fieldErrors)}
          maxLength={300}
          rows={3}
          defaultValue={account.address?.deliveryNotes ?? ""}
        />
      </Field>

      {error && (
        <p className="text-sm text-brick" role="alert">
          {error}
        </p>
      )}

      <p aria-live="polite" className="text-sm text-accent empty:hidden">
        {status}
      </p>

      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
        {onCancel && (
          <Button type="button" variant="ghost" disabled={pending} onClick={onCancel}>
            Cancelar
          </Button>
        )}
        <Button type="submit" disabled={pending}>
          {pending ? pendingLabel : submitLabel}
        </Button>
      </div>
    </form>
  );
}
