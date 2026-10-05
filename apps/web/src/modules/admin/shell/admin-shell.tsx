import Link from "next/link";
import type { ReactNode } from "react";
import { ChairMark } from "@/components/furniture-marks";
import { ThemeToggle } from "@/components/theme-toggle";
import { UserMenu } from "@/modules/auth/user-menu";
import { AdminNav } from "@/modules/admin/shell/admin-nav";

export function AdminShell({ email, children }: { email: string; children: ReactNode }) {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-20 border-b border-hairline bg-surface-raised/90 backdrop-blur supports-[backdrop-filter]:bg-surface-raised/75">
        <div className="flex items-center gap-4 px-6 py-3">
          <Link
            href="/admin"
            className="flex items-center gap-2 font-display text-lg font-semibold tracking-tight text-ink"
          >
            <ChairMark className="h-6 w-6 text-accent" />
            Furnistore
            <span className="rounded-sm bg-accent/15 px-1.5 py-0.5 font-body text-xs font-medium text-accent">
              Panel
            </span>
          </Link>
          <div className="ml-auto flex items-center gap-3">
            <Link href="/" className="text-sm text-ink-muted transition-colors hover:text-accent">
              Ver tienda
            </Link>
            <ThemeToggle />
            <UserMenu email={email} isAdmin />
          </div>
        </div>
      </header>

      <div className="flex flex-1 flex-col lg:flex-row">
        <aside className="border-b border-hairline bg-surface-raised px-4 py-3 lg:w-56 lg:shrink-0 lg:border-r lg:border-b-0 lg:py-6">
          <AdminNav />
        </aside>
        <div className="min-w-0 flex-1 px-6 py-8">{children}</div>
      </div>
    </div>
  );
}
