import type { Metadata } from "next";
import { notFound, redirect } from "next/navigation";
import type { ReactNode } from "react";
import { getSessionUser } from "@/lib/session";
import { AdminShell } from "@/modules/admin/shell/admin-shell";

export const metadata: Metadata = {
  title: { default: "Panel", template: "%s · Panel · Furnistore" },
  robots: { index: false, follow: false },
};

export default async function AdminLayout({ children }: { children: ReactNode }) {
  const user = await getSessionUser();

  if (!user) {
    redirect("/login?next=/admin");
  }

  if (!user.isAdmin) {
    notFound();
  }

  return <AdminShell email={user.email}>{children}</AdminShell>;
}
