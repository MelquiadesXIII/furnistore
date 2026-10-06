import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { Card, CardContent } from "@/components/ui/card";
import { AccountEditor } from "@/modules/account/account-editor";
import { saveAccount } from "@/modules/account/actions";
import type { Account } from "@/modules/account/types";

export function ProfileView({ account }: { account: Account }) {
  const fullName = `${account.firstName} ${account.lastName}`.trim();
  const initial = (account.firstName || account.email).charAt(0).toUpperCase();

  return (
    <div className="flex flex-col gap-8">
      <h1 className="font-display text-3xl font-semibold tracking-tight text-ink">Mi perfil</h1>

      <Card className="rounded-sm">
        <CardContent className="flex items-center gap-4">
          <Avatar size="lg">
            <AvatarFallback className="bg-accent/15 text-lg font-medium text-accent">
              {initial}
            </AvatarFallback>
          </Avatar>
          <div className="flex min-w-0 flex-col gap-0.5">
            {fullName && <span className="truncate font-medium text-ink">{fullName}</span>}
            <span className="truncate text-sm text-ink-muted">{account.email}</span>
          </div>
        </CardContent>
      </Card>

      <section className="flex flex-col gap-4">
        <div className="flex flex-col gap-1">
          <h2 className="font-display text-xl font-semibold text-ink">Datos de envío</h2>
          <p className="text-sm text-ink-muted">
            {account.isProfileComplete
              ? "Usamos estos datos para entregar tus pedidos."
              : "Completa tu teléfono y dirección para poder hacer pedidos."}
          </p>
        </div>

        <Card className="rounded-sm">
          <CardContent>
            <AccountEditor
              account={account}
              action={saveAccount.bind(null, "/profile")}
              submitLabel="Guardar cambios"
              savedMessage="Datos guardados."
            />
          </CardContent>
        </Card>
      </section>
    </div>
  );
}
