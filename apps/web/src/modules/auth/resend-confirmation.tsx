"use client";

import { useEffect, useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import { resendConfirmationEmail } from "@/modules/auth/actions";

const COOLDOWN_SECONDS = 60;

type Feedback = { tone: "success" | "error"; text: string };

export function ResendConfirmation({ email, sentRecently }: { email: string; sentRecently: boolean }) {
  const [secondsLeft, setSecondsLeft] = useState(sentRecently ? COOLDOWN_SECONDS : 0);
  const [feedback, setFeedback] = useState<Feedback | null>(null);
  const [pending, startTransition] = useTransition();

  useEffect(() => {
    if (secondsLeft <= 0) return;
    const timer = setTimeout(() => setSecondsLeft((seconds) => seconds - 1), 1000);
    return () => clearTimeout(timer);
  }, [secondsLeft]);

  function resend() {
    setFeedback(null);
    startTransition(async () => {
      const result = await resendConfirmationEmail(email);

      if (result.ok) {
        setFeedback({ tone: "success", text: "Te enviamos un enlace nuevo. Revisa también la carpeta de spam." });
        setSecondsLeft(COOLDOWN_SECONDS);
      } else {
        setFeedback({ tone: "error", text: result.message });
      }
    });
  }

  const label = pending
    ? "Enviando…"
    : secondsLeft > 0
      ? `Reenviar correo en ${secondsLeft} s`
      : "Reenviar correo de confirmación";

  return (
    <div className="flex flex-col gap-2">
      <Button
        type="button"
        variant="outline"
        className="w-full"
        disabled={pending || secondsLeft > 0}
        onClick={resend}
      >
        {label}
      </Button>
      <p
        aria-live="polite"
        className={feedback?.tone === "error" ? "text-sm text-brick" : "text-sm text-ink-muted"}
      >
        {feedback?.text}
      </p>
    </div>
  );
}
