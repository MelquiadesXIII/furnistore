import { User } from "lucide-react";
import Link from "next/link";
import { Suspense } from "react";
import { getSessionUser } from "@/lib/session";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { ThemeToggle } from "@/components/theme-toggle";
import { ChairMark } from "@/components/furniture-marks";
import { UserMenu } from "@/modules/auth/user-menu";
import { CartBadge, CartLink } from "@/modules/cart/cart-badge";
import { ProductSearchBar } from "@/modules/products/product-search-bar";

export async function SiteHeader() {
  const user = await getSessionUser();

  return (
    <header className="sticky top-0 z-20 border-b border-hairline bg-surface-raised/90 backdrop-blur supports-[backdrop-filter]:bg-surface-raised/75">
      <div className="mx-auto flex max-w-6xl items-center gap-6 px-6 py-4">
        <Link
          href="/"
          className="flex shrink-0 items-center gap-2 font-display text-lg font-semibold tracking-tight text-ink"
        >
          <ChairMark className="h-6 w-6 text-accent" />
          Furnistore
        </Link>

        <div className="flex flex-1 justify-center">
          <Suspense fallback={<div className="h-8 w-full max-w-md" />}>
            <ProductSearchBar />
          </Suspense>
        </div>

        <div className="flex shrink-0 items-center gap-3">
          <ThemeToggle />
          {user ? (
            <Suspense fallback={<CartLink count={null} />}>
              <CartBadge />
            </Suspense>
          ) : (
            <CartLink count={null} />
          )}
          {user ? (
            <UserMenu email={user.email} />
          ) : (
            <Link
              href="/login"
              aria-label="Iniciar sesión"
              className="rounded-full transition-opacity hover:opacity-80 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
            >
              <Avatar>
                <AvatarFallback className="bg-muted text-ink-muted">
                  <User className="h-4 w-4" />
                </AvatarFallback>
              </Avatar>
            </Link>
          )}
        </div>
      </div>
    </header>
  );
}
