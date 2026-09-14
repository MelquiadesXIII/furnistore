import Link from "next/link";
import { Button } from "@/components/ui/button";
import { ChairMark } from "@/components/furniture-marks";


export default function NotFound() {
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-4 px-6 py-32 text-center">
      <ChairMark className="h-16 w-16 text-ink-muted" />
      <h1 className="font-display text-2xl font-semibold text-ink">
        Esta pieza no existe
      </h1>
      <p className="text-sm text-ink-muted">
        Quizás la movieron de sitio o ya no está disponible.
      </p>
      <Button asChild>
        <Link href="/">Volver al catálogo</Link>
      </Button>
    </div>
  );
}