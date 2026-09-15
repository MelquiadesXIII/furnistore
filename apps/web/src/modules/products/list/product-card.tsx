import Image from "next/image";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardTitle } from "@/components/ui/card";
import { FURNITURE_MARKS } from "@/components/furniture-marks";
import type { Product } from "@/modules/products/types";

function formatPrice(value: number) {
  return `$${value.toLocaleString("es", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

export function ProductCard({
  product,
  isAuthenticated,
}: {
  product: Product;
  isAuthenticated: boolean;
}) {
  const Mark = FURNITURE_MARKS[product.id % FURNITURE_MARKS.length];
  const image = product.imageUrl;

  return (
    <li>
      <Card className="gap-0 overflow-hidden rounded-sm py-0 transition-colors hover:border-accent">
        <div className="relative flex aspect-square items-center justify-center overflow-hidden bg-surface">
          {image ? (
            <Image
              src={image}
              alt={product.name}
              fill
              sizes="(min-width: 1280px) 25vw, (min-width: 1024px) 33vw, (min-width: 640px) 50vw, 100vw"
              className="object-cover"
            />
          ) : (
            <Mark className="h-full w-full p-8 text-ink-muted transition-colors" />
          )}
        </div>
        <CardContent className="flex flex-col gap-3 border-t border-hairline px-4 py-3">
          <div className="flex flex-col gap-1">
            <CardTitle className="font-display text-base font-medium text-ink">
              {product.name}
            </CardTitle>
            <span className="font-mono text-sm text-ink-muted">{formatPrice(product.price)}</span>
          </div>
          {isAuthenticated ? (
            <Button disabled title="Disponible próximamente" className="w-full">
              Comprar
            </Button>
          ) : (
            <Button asChild className="w-full">
              <Link href="/login">Comprar</Link>
            </Button>
          )}
        </CardContent>
      </Card>
    </li>
  );
}
