import { CircleAlert, CircleCheck } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import type { ConfirmationOutcome } from "@/modules/auth/email-confirmation/outcomes";
import { RedirectCountdown } from "@/modules/auth/email-confirmation/redirect-countdown";

const REDIRECT_SECONDS = 5;

export function EmailConfirmationView({ outcome }: { outcome: ConfirmationOutcome }) {
  const Icon = outcome.succeeded ? CircleCheck : CircleAlert;

  return (
    <div className="flex flex-col gap-5">
      <Icon
        aria-hidden="true"
        className={`size-10 ${outcome.succeeded ? "text-accent" : "text-brick"}`}
      />
      <p className="text-ink-muted">{outcome.message}</p>
      {outcome.succeeded && <RedirectCountdown href={outcome.action.href} seconds={REDIRECT_SECONDS} />}
      <Button asChild className="w-full">
        <Link href={outcome.action.href}>{outcome.action.label}</Link>
      </Button>
    </div>
  );
}
