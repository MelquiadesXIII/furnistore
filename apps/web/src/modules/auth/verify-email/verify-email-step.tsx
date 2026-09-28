"use client";

import { MailCheck } from "lucide-react";
import { useState, useTransition, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { verifyEmailCode } from "@/modules/auth/actions";
import { ResendConfirmation } from "@/modules/auth/resend-confirmation";

export function VerifyEmailStep({
  email,
  emailSent,
  onVerified,
}: {
  email: string;
  emailSent: boolean;
  onVerified: () => Promise<void> | void;
}) {
  const [code, setCode] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError(null);

    startTransition(async () => {
      const result = await verifyEmailCode(email, code);

      if (!result.ok) {
        setError(result.message);
        return;
      }

      await onVerified();
    });
  }

  return (
    <div className="flex flex-col gap-5">
      <MailCheck aria-hidden="true" className="size-10 text-accent" />

      <div className="flex flex-col gap-2">
        <h2 className="font-display text-xl font-semibold text-ink">Confirma tu correo</h2>
        {emailSent ? (
          <p className="text-sm text-ink-muted">
            Te enviamos un código de 6 dígitos a{" "}
            <span className="font-medium break-all text-ink">{email}</span>. Escríbelo aquí para
            activar tu cuenta.
          </p>
        ) : (
          <p className="text-sm text-ink-muted">
            Escribe el código de 6 dígitos que enviamos a{" "}
            <span className="font-medium break-all text-ink">{email}</span>, o pide uno nuevo.
          </p>
        )}
      </div>

      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="code" className="text-ink-muted">
            Código de verificación
          </Label>
          <Input
            id="code"
            name="code"
            value={code}
            onChange={(event) => setCode(event.target.value.replace(/\D/g, "").slice(0, 6))}
            inputMode="numeric"
            autoComplete="one-time-code"
            pattern="\d{6}"
            maxLength={6}
            required
            autoFocus
            placeholder="123456"
            aria-invalid={error ? true : undefined}
            className="bg-surface-raised text-center font-mono text-2xl tracking-[0.5em]"
          />
        </div>

        {error && (
          <p className="text-sm text-brick" role="alert">
            {error}
          </p>
        )}

        <Button type="submit" disabled={pending || code.length !== 6} className="w-full">
          {pending ? "Verificando…" : "Confirmar correo"}
        </Button>
      </form>

      <ResendConfirmation email={email} sentRecently={emailSent} />

      <p className="text-xs text-ink-muted">
        El código vence en 15 minutos. Revisa también la carpeta de spam.
      </p>
    </div>
  );
}
