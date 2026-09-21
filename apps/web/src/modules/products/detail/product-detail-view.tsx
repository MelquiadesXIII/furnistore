import { ArrowLeft } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { FURNITURE_MARKS } from "@/components/furniture-marks";
import { formatPrice } from "@/lib/format-price";
import { AddToCartButton } from "@/modules/cart/add-to-cart-button";
import { ProductSpecs } from "@/modules/products/detail/product-specs";
import { buildProductHref } from "@/modules/products/slug";
import type { Product } from "@/modules/products/types";

const LOW_STOCK = 5;

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
          <div className="flex flex-col gap-1">
            <span className="text-xs font-medium tracking-wide text-ink-muted uppercase">
              {product.category.name}
            </span>
            <h1 className="font-display text-3xl font-semibold tracking-tight text-ink">
              {product.name}
            </h1>
          </div>
          <span className="font-mono text-xl text-ink-muted">{formatPrice(product.price)}</span>
          {!product.isActive ? (
            <span className="text-sm text-brick">Este producto ya no está disponible.</span>
          ) : (
            product.stock > 0 &&
            product.stock <= LOW_STOCK && (
              <span className="text-sm text-brick">
                {product.stock === 1 ? "¡Última unidad!" : `¡Quedan ${product.stock} unidades!`}
              </span>
            )
          )}
          {product.description && (
            <p className="leading-relaxed whitespace-pre-line text-ink-muted">
              {product.description}
            </p>
          )}
          <ProductSpecs product={product} />
          {product.isActive && (
            <div className="max-w-sm">
              <AddToCartButton
                productId={product.id}
                productName={product.name}
                stock={product.stock}
                isAuthenticated={isAuthenticated}
                returnTo={buildProductHref(product)}
                withQuantity
              />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
