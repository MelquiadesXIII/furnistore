import { Badge } from "@/components/ui/badge";

export function CustomerBadges({
  isAdmin,
  isDisabled,
  isLockedOut,
  emailConfirmed,
}: {
  isAdmin: boolean;
  isDisabled: boolean;
  isLockedOut: boolean;
  emailConfirmed: boolean;
}) {
  return (
    <span className="flex flex-wrap gap-1">
      {isAdmin && <Badge className="rounded-sm">Admin</Badge>}
      {isDisabled && (
        <Badge variant="outline" className="rounded-sm bg-brick/10 text-brick">
          Desactivada
        </Badge>
      )}
      {isLockedOut && (
        <Badge variant="outline" className="rounded-sm border-brick text-brick">
          Bloqueada
        </Badge>
      )}
      {!emailConfirmed && (
        <Badge variant="outline" className="rounded-sm text-ink-muted">
          Sin confirmar
        </Badge>
      )}
    </span>
  );
}
