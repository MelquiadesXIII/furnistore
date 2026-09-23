"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { EMAIL_NOT_CONFIRMED } from "@/modules/auth/error-messages";
import { LoginForm } from "@/modules/auth/login/login-form";
import { ResendConfirmation } from "@/modules/auth/resend-confirmation";
import type { AuthFailure } from "@/modules/auth/types";

const UNREACHABLE: AuthFailure = {
  code: null,
  message: "No se pudo conectar con el servidor. Intenta de nuevo en un momento.",
};

export function LoginContainer({ next }: { next: string }) {
  const router = useRouter();
  const [failure, setFailure] = useState<AuthFailure | null>(null);
  const [email, setEmail] = useState("");
  const [pending, setPending] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setFailure(null);

    const formData = new FormData(event.currentTarget);
    const submittedEmail = String(formData.get("email") ?? "");

    const res = await fetch("/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email: submittedEmail, password: formData.get("password") }),
    }).catch(() => null);

    if (res?.ok) {
      router.push(next);
      router.refresh();
      return;
    }

    const data = await res?.json().catch(() => null);
    setEmail(submittedEmail);
    setFailure(data?.error ?? UNREACHABLE);
    setPending(false);
  }

  return (
    <LoginForm pending={pending} error={failure?.message ?? null} onSubmit={handleSubmit}>
      {failure?.code === EMAIL_NOT_CONFIRMED && (
        <ResendConfirmation key={email} email={email} sentRecently={false} />
      )}
    </LoginForm>
  );
}
