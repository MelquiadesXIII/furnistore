import { redirect } from "next/navigation";
import { Panel } from "@/components/panel";
import { getSessionUser } from "@/lib/session";
import { getAccount } from "@/modules/account/api";
import { accountErrorMessage } from "@/modules/account/error-messages";
import { ProfileView } from "@/modules/account/profile/profile-view";

export async function ProfileContainer() {
  const [user, result] = await Promise.all([getSessionUser(), getAccount()]);

  if (!user || (!result.ok && result.error.kind === "unauthorized")) {
    redirect("/login?next=/profile");
  }

  return (
    <div className="mx-auto w-full max-w-2xl flex-1 px-6 py-10">
      {result.ok ? (
        <ProfileView account={result.value} />
      ) : (
        <div className="flex flex-col gap-8">
          <h1 className="font-display text-3xl font-semibold tracking-tight text-ink">Mi perfil</h1>
          <Panel>{accountErrorMessage(result.error)}</Panel>
        </div>
      )}
    </div>
  );
}
