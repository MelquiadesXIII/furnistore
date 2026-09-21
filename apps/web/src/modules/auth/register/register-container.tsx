"use client";

import { useRouter } from "next/navigation";
import { useState, type FormEvent } from "react";
import { translateAuthError } from "@/modules/auth/error-messages";
import { RegisterForm } from "@/modules/auth/register/register-form";

export function RegisterContainer() {
  const router = useRouter();
  const [errors, setErrors] = useState<string[]>([]);
  const [pending, setPending] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending(true);
    setErrors([]);

    const formData = new FormData(event.currentTarget);

    const res = await fetch("/api/auth/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        firstName: formData.get("firstName"),
        lastName: formData.get("lastName"),
        emailAddress: formData.get("emailAddress"),
        password: formData.get("password"),
      }),
    });

    if (res.ok) {
      router.push("/");
      router.refresh();
      return;
    }

    const data = await res.json().catch(() => ({ errors: ["Ocurrió un error inesperado."] }));
    setErrors((data.errors ?? []).map(translateAuthError));
    setPending(false);
  }

  return <RegisterForm pending={pending} errors={errors} onSubmit={handleSubmit} />;
}
