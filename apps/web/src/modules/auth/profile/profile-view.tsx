import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Card, CardContent } from "@/components/ui/card";

export function ProfileView({ email }: { email: string }) {
  return (
    <div className="flex flex-col gap-8">
      <h1 className="font-display text-3xl font-semibold tracking-tight text-ink">Mi perfil</h1>

      <Card className="rounded-sm">
        <CardContent className="flex items-center gap-4">
          <Avatar size="lg">
            <AvatarFallback className="bg-accent/15 text-lg font-medium text-accent">
              {email.charAt(0).toUpperCase()}
            </AvatarFallback>
          </Avatar>
          <div className="flex flex-col gap-0.5">
            <span className="text-xs font-medium tracking-wide text-ink-muted uppercase">
              Correo electrónico
            </span>
            <span className="text-base text-ink">{email}</span>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
