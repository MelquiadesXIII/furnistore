"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import {
  INVALID_IDENTITY_CARD,
  isValidIdentityCard,
  isValidPersonName,
  normalizeIdentityCard,
  normalizePersonName,
} from "@/modules/account/account-validation";
import { EMAIL_PATTERN, INVALID_EMAIL } from "@/modules/auth/email";
import { RegisterForm } from "@/modules/auth/register/register-form";
import { signIn, UNREACHABLE } from "@/modules/auth/sign-in";
import type { AuthFailure } from "@/modules/auth/types";
import { VerifyEmailStep } from "@/modules/auth/verify-email/verify-email-step";

type Registered = { email: string; password: string; emailSent: boolean };

const INVALID_NAME: AuthFailure = {
  code: null,
  message: "En nombre y apellidos usa solo letras, separadas por un espacio, un guion o un apóstrofo.",
};

const UNCONFIRMED: AuthFailure = {
  code: null,
  message: "No pudimos confirmar el registro. Vuelve a pulsar «Crear cuenta»: si tu cuenta ya se creó, te enviaremos el código.",
};

export function RegisterContainer() {
  const router = useRouter();
  const [failure, setFailure] = useState<AuthFailure | null>(null);
  const [registered, setRegistered] = useState<Registered | null>(null);
  const [pending, setPending] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const formData = new FormData(event.currentTarget);
    const firstName = normalizePersonName(String(formData.get("firstName") ?? ""));
    const lastName = normalizePersonName(String(formData.get("lastName") ?? ""));
    const identityCard = normalizeIdentityCard(String(formData.get("identityCard") ?? ""));
    const email = String(formData.get("emailAddress") ?? "").trim();
    const password = String(formData.get("password") ?? "");

    if (!isValidPersonName(firstName) || !isValidPersonName(lastName)) {
      setFailure(INVALID_NAME);
      return;
    }

    if (!isValidIdentityCard(identityCard)) {
      setFailure({ code: null, message: INVALID_IDENTITY_CARD });
      return;
    }

    if (!EMAIL_PATTERN.test(email)) {
      setFailure({ code: null, message: INVALID_EMAIL });
      return;
    }

    setPending(true);
    setFailure(null);

    const res = await fetch("/api/auth/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ firstName, lastName, identityCard, emailAddress: email, password }),
    }).catch(() => null);

    const data = await res?.json().catch(() => null);

    if (res?.ok) {
      setRegistered({ email, password, emailSent: Boolean(data?.emailSent) });
      return;
    }

    setFailure(!res ? UNREACHABLE : res.status >= 500 ? UNCONFIRMED : (data?.error ?? UNREACHABLE));
    setPending(false);
  }

  async function handleVerified() {
    if (!registered) return;

    const result = await signIn(registered.email, registered.password);

    if (result.ok) {
      router.push("/");
      router.refresh();
    } else {
      router.push("/login");
    }
  }

  if (registered) {
    return (
      <VerifyEmailStep
        email={registered.email}
        emailSent={registered.emailSent}
        onVerified={handleVerified}
      />
    );
  }

  return <RegisterForm pending={pending} error={failure?.message ?? null} onSubmit={handleSubmit} />;
}
