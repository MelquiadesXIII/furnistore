import { ArrowLeft } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { FURNITURE_MARKS } from "@/components/furniture-marks";
import { ComprarButton } from "@/modules/products/comprar-button";
import { formatPrice } from "@/modules/products/format-price";
import type { Product } from "@/modules/products/types";

export function ProductDetailView({
  product,
  isAuthenticated,
}: {
  product: Product;
  isAuthenticated: boolean;
}) {
  const Mark = FURNITURE_MARKS[product.id % FURNITURE_MARKS.length];
  const image = product.imageUrl;

  return (
    <div className="flex flex-col gap-8">
      <Link
        href="/"
        className="inline-flex w-fit items-center gap-1.5 text-sm text-ink-muted transition-colors hover:text-ink"
      >
        <ArrowLeft className="h-4 w-4" />
        Volver al catálogo
      </Link>

      <div className="grid gap-8 md:grid-cols-2">
        <div className="relative flex aspect-square items-center justify-center overflow-hidden rounded-sm border border-hairline bg-surface">
          {image ? (
            <Image
              src={image}
              alt={product.name}
              fill
              sizes="(min-width: 768px) 50vw, 100vw"
              className="object-cover"
              priority
            />
          ) : (
            <Mark className="h-full w-full p-16 text-ink-muted" />
          )}
        </div>

        <div className="flex flex-col gap-4">
          <h1 className="font-display text-3xl font-semibold tracking-tight text-ink">
            {product.name}
          </h1>
          <span className="font-mono text-xl text-ink-muted">{formatPrice(product.price)}</span>
          <div className="max-w-xs">
            <ComprarButton isAuthenticated={isAuthenticated} />
          </div>
        </div>
      </div>
    </div>
  );
}
