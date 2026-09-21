import type { FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export function RegisterForm({
  pending,
  errors,
  onSubmit,
}: {
  pending: boolean;
  errors: string[];
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
}) {
  return (
    <form onSubmit={onSubmit} className="flex w-full flex-col gap-5">
      <div className="grid gap-5 sm:grid-cols-2">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="firstName" className="text-ink-muted">
            Nombre
          </Label>
          <Input
            id="firstName"
            name="firstName"
            type="text"
            required
            minLength={2}
            maxLength={60}
            autoComplete="given-name"
            className="bg-surface-raised"
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="lastName" className="text-ink-muted">
            Apellidos
          </Label>
          <Input
            id="lastName"
            name="lastName"
            type="text"
            required
            minLength={2}
            maxLength={60}
            autoComplete="family-name"
            className="bg-surface-raised"
          />
        </div>
      </div>

      <div className="flex flex-col gap-1.5">
        <Label htmlFor="emailAddress" className="text-ink-muted">
          Correo electrónico
        </Label>
        <Input
          id="emailAddress"
          name="emailAddress"
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
          minLength={8}
          autoComplete="new-password"
          className="bg-surface-raised"
        />
        <p className="text-xs text-ink-muted">Mínimo 8 caracteres.</p>
      </div>

      {errors.length > 0 && (
        <ul className="flex flex-col gap-1 text-sm text-brick" role="alert">
          {errors.map((err) => (
            <li key={err}>{err}</li>
          ))}
        </ul>
      )}

      <Button type="submit" disabled={pending} className="mt-2 w-full">
        {pending ? "Creando cuenta…" : "Crear cuenta"}
      </Button>
    </form>
  );
}
