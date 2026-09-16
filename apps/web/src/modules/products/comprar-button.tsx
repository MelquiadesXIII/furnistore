import Link from "next/link";
import { Button } from "@/components/ui/button";

export function ComprarButton({ isAuthenticated }: { isAuthenticated: boolean }) {
  if (isAuthenticated) {
    return (
      <Button disabled title="Disponible próximamente" className="w-full">
        Comprar
      </Button>
    );
  }

  return (
    <Button asChild className="w-full">
      <Link href="/login">Comprar</Link>
    </Button>
  );
}
