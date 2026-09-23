import { MailCheck } from "lucide-react";
import Link from "next/link";
import { ResendConfirmation } from "@/modules/auth/resend-confirmation";

export function CheckEmail({ email, emailSent }: { email: string; emailSent: boolean }) {
  return (
    <div className="flex flex-col gap-5">
      <MailCheck aria-hidden="true" className="size-10 text-accent" />

      <div className="flex flex-col gap-2">
        <h2 className="font-display text-xl font-semibold text-ink">Revisa tu correo</h2>
        {emailSent ? (
          <p className="text-sm text-ink-muted">
            Te enviamos un enlace de confirmación a{" "}
            <span className="font-medium break-all text-ink">{email}</span>. Ábrelo para activar tu
            cuenta y luego inicia sesión.
          </p>
        ) : (
          <p className="text-sm text-brick" role="alert">
            Tu cuenta se creó, pero no pudimos enviarte el correo de confirmación. Pide uno nuevo en
            unos minutos.
          </p>
        )}
        <p className="text-sm text-ink-muted">¿No te llegó? Revisa la carpeta de spam o pide otro.</p>
      </div>

      <ResendConfirmation email={email} sentRecently={emailSent} />

      <Link href="/login" className="text-sm font-medium text-accent hover:underline">
        Ir a iniciar sesión
      </Link>
    </div>
  );
}
