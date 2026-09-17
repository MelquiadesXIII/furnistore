import { redirect } from "next/navigation";
import { getSessionUser } from "@/lib/session";
import { ProfileView } from "@/modules/auth/profile/profile-view";

export async function ProfileContainer() {
  const user = await getSessionUser();

  if (!user) {
    redirect("/login");
  }

  return (
    <div className="mx-auto w-full max-w-2xl flex-1 px-6 py-10">
      <ProfileView email={user.email} />
    </div>
  );
}
