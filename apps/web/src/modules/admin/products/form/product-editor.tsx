"use client";

import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import { Button } from "@/components/ui/button";
import type { ProductFormResult } from "@/modules/admin/products/actions";
import { ProductForm } from "@/modules/admin/products/form/product-form";
import type { AdminProduct } from "@/modules/admin/products/types";

export function ProductEditor({
  product,
  categories,
  action,
}: {
  product: AdminProduct;
  categories: { id: number; name: string }[];
  action: (formData: FormData) => Promise<ProductFormResult>;
}) {
  const router = useRouter();
  const [saved, setSaved] = useState<string | null>(null);
  const [stale, setStale] = useState(false);
  const [reloading, startReload] = useTransition();

  return (
    <div className="flex flex-col gap-4">
      {saved && (
        <p role="status" className="rounded-sm border border-accent/40 bg-accent/10 px-3 py-2 text-sm text-ink">
          {saved}
        </p>
      )}
      {stale && (
        <div className="flex flex-wrap items-center justify-between gap-3 rounded-sm border border-brick/40 px-3 py-2">
          <p className="text-sm text-ink">Otra persona guardó cambios en este producto.</p>
          <Button
            type="button"
            size="sm"
            variant="outline"
            disabled={reloading}
            onClick={() =>
              startReload(() => {
                setStale(false);
                router.refresh();
              })
            }
          >
            Cargar la versión actual
          </Button>
        </div>
      )}
      <ProductForm
        key={product.version}
        product={product}
        categories={categories}
        action={action}
        submitLabel="Guardar cambios"
        onResult={(result) => {
          setSaved(result.ok ? result.message : null);
          setStale(!result.ok && Boolean(result.stale));
        }}
      />
    </div>
  );
}
