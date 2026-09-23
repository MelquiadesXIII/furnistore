import type { FormEvent, ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export function LoginForm({
  pending,
  error,
  onSubmit,
  children,
}: {
  pending: boolean;
  error: string | null;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  children?: ReactNode;
}) {
  return (
    <form onSubmit={onSubmit} className="flex w-full flex-col gap-5">
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="email" className="text-ink-muted">
          Correo electrónico
        </Label>
        <Input
          id="email"
          name="email"
          type="email"
          required
          autoComplete="email"
          className="bg-surface-raised"
        />
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="password" className="text-ink-muted">
          Contraseña
        </Label>
        <Input
          id="password"
          name="password"
          type="password"
          required
          autoComplete="current-password"
          className="bg-surface-raised"
        />
      </div>

      {error && (
        <p className="text-sm text-brick" role="alert">
          {error}
        </p>
      )}

      <Button type="submit" disabled={pending} className="mt-2 w-full">
        {pending ? "Entrando…" : "Entrar"}
      </Button>

      {children}
    </form>
  );
}
