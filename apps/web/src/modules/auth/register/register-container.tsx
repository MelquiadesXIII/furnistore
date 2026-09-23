"use client";

import { useState, type FormEvent } from "react";
import { CheckEmail } from "@/modules/auth/register/check-email";
import { RegisterForm } from "@/modules/auth/register/register-form";
import type { AuthFailure } from "@/modules/auth/types";

const UNREACHABLE: AuthFailure = {
  code: null,
  message: "No se pudo conectar con el servidor. Intenta de nuevo en un momento.",
};

type Registered = { email: string; emailSent: boolean };

export function RegisterContainer() {
  const [failure, setFailure] = useState<AuthFailure | null>(null);
  const [registered, setRegistered] = useState<Registered | null>(null);
  const [pending, setPending] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setFailure(null);

    const formData = new FormData(event.currentTarget);
    const email = String(formData.get("emailAddress") ?? "").trim();

    const res = await fetch("/api/auth/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        firstName: formData.get("firstName"),
        lastName: formData.get("lastName"),
        emailAddress: email,
        password: formData.get("password"),
      }),
    }).catch(() => null);

    const data = await res?.json().catch(() => null);

    if (res?.ok) {
      setRegistered({ email, emailSent: Boolean(data?.emailSent) });
      return;
    }

    setFailure(data?.error ?? UNREACHABLE);
    setPending(false);
  }

  if (registered) {
    return <CheckEmail email={registered.email} emailSent={registered.emailSent} />;
  }

  return <RegisterForm pending={pending} error={failure?.message ?? null} onSubmit={handleSubmit} />;
}
