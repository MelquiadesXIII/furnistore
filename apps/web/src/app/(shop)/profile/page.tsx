import type { Metadata } from "next";
import { ProfileContainer } from "@/modules/account/profile/profile-container";

export const metadata: Metadata = {
  title: "Mi perfil",
  robots: { index: false, follow: false },
};

export default function Page() {
  return <ProfileContainer />;
}
