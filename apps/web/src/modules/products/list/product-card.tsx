import { ArrowUpRight } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { Card, CardContent, CardTitle } from "@/components/ui/card";
import { FURNITURE_MARKS } from "@/components/furniture-marks";
import { ComprarButton } from "@/modules/products/comprar-button";
import { formatPrice } from "@/modules/products/format-price";
import { buildProductHref } from "@/modules/products/slug";
import type { Product } from "@/modules/products/types";

export function ProductCard({
  product,
  isAuthenticated,
}: {
  product: Product;
  isAuthenticated: boolean;
}) {
  const Mark = FURNITURE_MARKS[product.id % FURNITURE_MARKS.length];
  const image = product.imageUrl;
  const href = buildProductHref(product);

  return (
    <li>
      <Card className="gap-0 overflow-hidden rounded-sm py-0 transition-colors hover:border-accent">
        <Link
          href={href}
          aria-label={`Ver detalles de ${product.name}`}
          className="group relative flex aspect-[4/5] items-center justify-center overflow-hidden bg-surface"
        >
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
          <span
            aria-hidden="true"
            className="absolute top-2 right-2 flex h-8 w-8 items-center justify-center rounded-full bg-surface-raised/90 text-ink shadow-sm backdrop-blur transition-colors group-hover:bg-accent group-hover:text-accent-ink"
          >
            <ArrowUpRight className="h-4 w-4" />
          </span>
        </Link>
        <CardContent className="flex flex-col gap-3 border-t border-hairline px-4 py-3">
          <div className="flex flex-col gap-1">
            <Link href={href}>
              <CardTitle className="font-display text-base font-medium text-ink transition-colors hover:text-accent">
                {product.name}
              </CardTitle>
            </Link>
            <span className="font-mono text-sm text-ink-muted">{formatPrice(product.price)}</span>
          </div>
          <ComprarButton isAuthenticated={isAuthenticated} />
        </CardContent>
      </Card>
    </li>
  );
}
