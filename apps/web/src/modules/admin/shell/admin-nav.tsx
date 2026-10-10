"use client";

import { ChartColumn, ClipboardList, FolderTree, History, LayoutDashboard, Package, Users } from "lucide-react";
import { cn } from "cn";
import Link from "next/link";
import { usePathname } from "next/navigation";

const LINKS = [
  { href: "/admin", label: "Resumen", icon: LayoutDashboard, exact: true },
  { href: "/admin/orders", label: "Pedidos", icon: ClipboardList },
  { href: "/admin/products", label: "Productos", icon: Package },
  { href: "/admin/categories", label: "Categorías", icon: FolderTree },
  { href: "/admin/customers", label: "Clientes", icon: Users },
  { href: "/admin/reports", label: "Reportes", icon: ChartColumn },
  { href: "/admin/audit", label: "Auditoría", icon: History },
];

export function AdminNav() {
  const pathname = usePathname();

  return (
    <nav aria-label="Panel de administración" className="flex gap-1 overflow-x-auto lg:flex-col">
      {LINKS.map(({ href, label, icon: Icon, exact }) => {
        const active = exact ? pathname === href : pathname === href || pathname.startsWith(`${href}/`);

        return (
          <Link
            key={href}
            href={href}
            aria-current={active ? "page" : undefined}
            className={cn(
              "flex shrink-0 items-center gap-2 rounded-sm px-3 py-2 text-sm text-ink-muted transition-colors hover:bg-surface hover:text-ink",
              active && "bg-surface font-medium text-ink",
            )}
          >
            <Icon className="size-4" />
            {label}
          </Link>
        );
      })}
    </nav>
  );
}
