"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { EMAIL_NOT_CONFIRMED } from "@/modules/auth/error-messages";
import { LoginForm } from "@/modules/auth/login/login-form";
import { signIn } from "@/modules/auth/sign-in";
import type { AuthFailure } from "@/modules/auth/types";
import { VerifyEmailStep } from "@/modules/auth/verify-email/verify-email-step";

type Credentials = { email: string; password: string };

export function LoginContainer({ next }: { next: string }) {
  const router = useRouter();
  const [failure, setFailure] = useState<AuthFailure | null>(null);
  const [unconfirmed, setUnconfirmed] = useState<Credentials | null>(null);
  const [pending, setPending] = useState(false);

  function finish() {
    router.push(next);
    router.refresh();
  }

  async function attempt(credentials: Credentials): Promise<AuthFailure | null> {
    const result = await signIn(credentials.email, credentials.password);

    if (result.ok) {
      finish();
      return null;
    }

    return result.failure;
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setFailure(null);

    const formData = new FormData(event.currentTarget);
    const credentials = {
      email: String(formData.get("email") ?? ""),
      password: String(formData.get("password") ?? ""),
    };

    const failed = await attempt(credentials);
    if (!failed) return;

    if (failed.code === EMAIL_NOT_CONFIRMED) {
      setUnconfirmed(credentials);
    } else {
      setFailure(failed);
    }
    setPending(false);
  }

  async function handleVerified() {
    if (!unconfirmed) return;

    const failed = await attempt(unconfirmed);
    if (failed) {
      setUnconfirmed(null);
      setFailure(failed);
    }
  }

  if (unconfirmed) {
    return (
      <VerifyEmailStep email={unconfirmed.email} emailSent={false} onVerified={handleVerified} />
    );
  }

  return <LoginForm pending={pending} error={failure?.message ?? null} onSubmit={handleSubmit} />;
}
