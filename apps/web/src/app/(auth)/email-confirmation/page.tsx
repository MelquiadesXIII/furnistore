import type { Metadata } from "next";
import Link from "next/link";
import loginHero from "@/assets/login-hero.jpg";
import { AuthShell } from "@/modules/auth/auth-shell";
import { EmailConfirmationView } from "@/modules/auth/email-confirmation/email-confirmation-view";
import { confirmationOutcome } from "@/modules/auth/email-confirmation/outcomes";

export const metadata: Metadata = {
  title: "Confirmación de correo",
  robots: { index: false },
};

export default async function EmailConfirmationPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string | string[] }>;
}) {
  const { status } = await searchParams;
  const outcome = confirmationOutcome(status);

  return (
    <AuthShell
      title={outcome.title}
      photo={loginHero}
      footer={
        <Link href="/" className="font-medium text-accent hover:underline">
          Volver al catálogo
        </Link>
      }
    >
      <EmailConfirmationView outcome={outcome} />
    </AuthShell>
  );
}
