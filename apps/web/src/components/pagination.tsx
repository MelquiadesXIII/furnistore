import Link from "next/link";
import { Button } from "@/components/ui/button";

export function Pagination({
  currentPage,
  totalPages,
  hrefFor,
}: {
  currentPage: number;
  totalPages: number;
  hrefFor: (page: number) => string;
}) {
  const hasPrev = currentPage > 1;
  const hasNext = currentPage < totalPages;

  return (
    <nav className="mt-10 flex items-center justify-center gap-4" aria-label="Paginación">
      {hasPrev ? (
        <Button variant="outline" size="sm" asChild>
          <Link href={hrefFor(currentPage - 1)}>Anterior</Link>
        </Button>
      ) : (
        <Button variant="outline" size="sm" disabled>
          Anterior
        </Button>
      )}

      <span className="font-mono text-sm text-ink-muted">
        Página {currentPage} de {totalPages}
      </span>

      {hasNext ? (
        <Button variant="outline" size="sm" asChild>
          <Link href={hrefFor(currentPage + 1)}>Siguiente</Link>
        </Button>
      ) : (
        <Button variant="outline" size="sm" disabled>
          Siguiente
        </Button>
      )}
    </nav>
  );
}
