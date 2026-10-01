import { cn } from "cn";
import Image from "next/image";
import { FURNITURE_MARKS } from "@/components/furniture-marks";
import type { OrderLine } from "@/modules/orders/types";

export function OrderLineThumb({
  line,
  pixels,
  className,
}: {
  line: OrderLine;
  pixels: number;
  className?: string;
}) {
  const Mark = FURNITURE_MARKS[line.productId % FURNITURE_MARKS.length];

  return (
    <span
      className={cn("relative block shrink-0 overflow-hidden rounded-sm bg-surface", className)}
      style={{ width: pixels, height: pixels }}
    >
      {line.imageUrl ? (
        <Image
          src={line.imageUrl}
          alt={line.productName}
          fill
          sizes={`${pixels}px`}
          className="object-cover"
        />
      ) : (
        <Mark className="h-full w-full p-2 text-ink-muted" />
      )}
    </span>
  );
}
