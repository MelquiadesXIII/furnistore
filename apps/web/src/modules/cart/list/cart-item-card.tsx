import { Minus, Plus, Trash2 } from "lucide-react";
import Image from "next/image";
import Link from "next/link";
import { FURNITURE_MARKS } from "@/components/furniture-marks";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { formatPrice } from "@/lib/format-price";
import type { CartItem } from "@/modules/cart/types";
import { buildProductHref } from "@/modules/products/slug";

export function CartItemCard({
  item,
  lineTotal,
  disabled,
  onQuantityChange,
  onRemove,
}: {
  item: CartItem;
  lineTotal: number | null;
  disabled: boolean;
  onQuantityChange: (quantity: number) => void;
  onRemove: () => void;
}) {
  const href = buildProductHref({ id: item.productId, name: item.productName });
  const Mark = FURNITURE_MARKS[item.productId % FURNITURE_MARKS.length];
  const unavailable = !item.isActive;
  const exceedsStock = item.quantity > item.stock;

  return (
    <li>
      <Card className="rounded-sm py-0">
        <CardContent className="flex gap-4 p-4">
          <Link
            href={href}
            aria-label={`Ver detalles de ${item.productName}`}
            className="relative size-24 shrink-0 overflow-hidden rounded-sm bg-surface"
          >
            {item.imageUrl ? (
              <Image
                src={item.imageUrl}
                alt={item.productName}
                fill
                sizes="96px"
                className="object-cover"
              />
            ) : (
              <Mark className="h-full w-full p-3 text-ink-muted" />
            )}
          </Link>

          <div className="flex min-w-0 flex-1 flex-col gap-3">
            <div className="flex items-start justify-between gap-4">
              <div className="flex min-w-0 flex-col gap-1">
                <Link
                  href={href}
                  className="truncate font-display font-medium text-ink transition-colors hover:text-accent"
                >
                  {item.productName}
                </Link>
                <span className="font-mono text-sm text-ink-muted">
                  {formatPrice(item.unitPrice)} c/u
                </span>
              </div>
              {lineTotal === null ? (
                <span className="font-mono text-ink-muted">…</span>
              ) : (
                <span className="font-mono text-ink">{formatPrice(lineTotal)}</span>
              )}
            </div>

            <div className="flex items-center justify-between gap-4">
              <div
                role="group"
                aria-label={`Cantidad de ${item.productName}`}
                className="flex items-center gap-1"
              >
                <Button
                  type="button"
                  variant="outline"
                  size="icon-sm"
                  aria-label="Quitar una unidad"
                  disabled={disabled || unavailable || item.quantity <= 1 || item.stock < 1}
                  onClick={() => onQuantityChange(Math.min(item.quantity - 1, item.stock))}
                >
                  <Minus />
                </Button>
                <span className="w-8 text-center font-mono tabular-nums text-ink">
                  {item.quantity}
                </span>
                <Button
                  type="button"
                  variant="outline"
                  size="icon-sm"
                  aria-label="Agregar una unidad"
                  disabled={disabled || unavailable || item.quantity >= item.stock}
                  onClick={() => onQuantityChange(item.quantity + 1)}
                >
                  <Plus />
                </Button>
              </div>

              <Button type="button" variant="ghost" size="sm" disabled={disabled} onClick={onRemove}>
                <Trash2 />
                Quitar
              </Button>
            </div>

            {unavailable ? (
              <p className="text-sm text-brick">
                Este producto ya no está disponible. Quítalo para continuar.
              </p>
            ) : (
              exceedsStock && (
                <p className="text-sm text-brick">
                  {item.stock === 0
                    ? "Este producto se agotó. Quítalo para continuar."
                    : `Solo quedan ${item.stock}. Ajusta la cantidad para continuar.`}
                </p>
              )
            )}
          </div>
        </CardContent>
      </Card>
    </li>
  );
}
