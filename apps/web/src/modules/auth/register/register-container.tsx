"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { RegisterForm } from "@/modules/auth/register/register-form";
import { signIn, UNREACHABLE } from "@/modules/auth/sign-in";
import type { AuthFailure } from "@/modules/auth/types";
import { VerifyEmailStep } from "@/modules/auth/verify-email/verify-email-step";

type Registered = { email: string; password: string; emailSent: boolean };

export function RegisterContainer() {
  const router = useRouter();
  const [failure, setFailure] = useState<AuthFailure | null>(null);
  const [registered, setRegistered] = useState<Registered | null>(null);
  const [pending, setPending] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setFailure(null);

    const formData = new FormData(event.currentTarget);
    const email = String(formData.get("emailAddress") ?? "").trim();
    const password = String(formData.get("password") ?? "");

    const res = await fetch("/api/auth/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        firstName: formData.get("firstName"),
        lastName: formData.get("lastName"),
        emailAddress: email,
        password,
      }),
    }).catch(() => null);

    const data = await res?.json().catch(() => null);

    if (res?.ok) {
      setRegistered({ email, password, emailSent: Boolean(data?.emailSent) });
      return;
    }

    setFailure(data?.error ?? UNREACHABLE);
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
