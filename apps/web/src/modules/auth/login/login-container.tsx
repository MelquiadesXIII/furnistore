"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { translateAuthError } from "@/modules/auth/error-messages";
import { LoginForm } from "@/modules/auth/login/login-form";

export function LoginContainer({ next }: { next: string }) {
  const router = useRouter();
  const [errors, setErrors] = useState<string[]>([]);
  const [pending, setPending] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setErrors([]);

    const formData = new FormData(event.currentTarget);

    const res = await fetch("/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        email: formData.get("email"),
        password: formData.get("password"),
      }),
    });

    if (res.ok) {
      router.push(next);
      router.refresh();
      return;
    }

    const data = await res.json().catch(() => ({ errors: ["Ocurrió un error inesperado."] }));
    setErrors((data.errors ?? []).map(translateAuthError));
    setPending(false);
  }

  return <LoginForm pending={pending} errors={errors} onSubmit={handleSubmit} />;
}
