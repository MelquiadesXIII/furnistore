"use client";

import { cn } from "cn";
import Image from "next/image";
import { useEffect, useRef, useState, useTransition, type ChangeEvent, type FormEvent, type ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Textarea } from "@/components/ui/textarea";
import type { ProductFormResult } from "@/modules/admin/products/actions";
import {
  IMAGE_TYPES,
  MAX_IMAGE_BYTES,
  type ProductField,
  type ProductFieldErrors,
} from "@/modules/admin/products/product-validation";
import type { AdminProduct } from "@/modules/admin/products/types";

function Field({
  id,
  label,
  error,
  hint,
  className,
  children,
}: {
  id: ProductField | "isActive";
  label: string;
  error?: string;
  hint?: string;
  className?: string;
  children: ReactNode;
}) {
  return (
    <div className={cn("flex flex-col gap-1.5", className)}>
      <Label htmlFor={id} className="text-ink-muted">
        {label}
      </Label>
      {children}
      {error ? (
        <p id={`${id}-error`} className="text-xs text-brick">
          {error}
        </p>
      ) : (
        hint && <p className="text-xs text-ink-muted">{hint}</p>
      )}
    </div>
  );
}

function invalid(id: ProductField, errors: ProductFieldErrors) {
  return errors[id]
    ? { "aria-invalid": true as const, "aria-describedby": `${id}-error` }
    : {};
}

function ImagePicker({ current, error }: { current: string | null; error?: string }) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [kept, setKept] = useState(current);
  const [preview, setPreview] = useState<string | null>(null);
  const [localError, setLocalError] = useState<string | null>(null);

  useEffect(() => {
    return () => {
      if (preview) URL.revokeObjectURL(preview);
    };
  }, [preview]);

  function handleChange(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    setLocalError(null);

    if (file && (!IMAGE_TYPES.includes(file.type) || file.size > MAX_IMAGE_BYTES)) {
      setLocalError("Elige una imagen JPG, PNG o WebP de hasta 5 MB.");
      event.target.value = "";
      setPreview(null);
      return;
    }

    setPreview(file ? URL.createObjectURL(file) : null);
  }

  function remove() {
    setKept(null);
    setPreview(null);
    if (inputRef.current) inputRef.current.value = "";
  }

  const shown = preview ?? kept;

  return (
    <Field id="image" label="Imagen (opcional)" error={localError ?? error} hint="JPG, PNG o WebP, hasta 5 MB.">
      <div className="flex items-start gap-4">
        <div className="relative size-28 shrink-0 overflow-hidden rounded-sm border border-hairline bg-surface">
          {shown ? (
            <Image src={shown} alt="Vista previa" fill sizes="112px" unoptimized className="object-cover" />
          ) : (
            <span className="flex h-full items-center justify-center text-xs text-ink-muted">Sin imagen</span>
          )}
        </div>
        <div className="flex min-w-0 flex-col items-start gap-2">
          <Input
            ref={inputRef}
            id="image"
            name="image"
            type="file"
            accept={IMAGE_TYPES.join(",")}
            onChange={handleChange}
            className="bg-surface-raised"
            aria-invalid={localError || error ? true : undefined}
          />
          <input type="hidden" name="imageUrl" value={kept ?? ""} />
          {shown && (
            <Button type="button" variant="ghost" size="sm" onClick={remove}>
              Quitar imagen
            </Button>
          )}
        </div>
      </div>
    </Field>
  );
}

export function ProductForm({
  product,
  categories,
  action,
  submitLabel,
  onResult,
}: {
  product?: AdminProduct;
  categories: { id: number; name: string }[];
  action: (formData: FormData) => Promise<ProductFormResult>;
  submitLabel: string;
  onResult?: (result: ProductFormResult) => void;
}) {
  const [errors, setErrors] = useState<ProductFieldErrors>({});
  const [message, setMessage] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    setMessage(null);

    startTransition(async () => {
      const result = await action(formData);
      setErrors(result.ok ? {} : result.fieldErrors);
      if (!result.ok) setMessage(result.message);
      onResult?.(result);
    });
  }

  return (
    <form onSubmit={handleSubmit} noValidate className="flex flex-col gap-5">
      <Field id="name" label="Nombre" error={errors.name}>
        <Input id="name" name="name" defaultValue={product?.name} maxLength={120} className="bg-surface-raised" {...invalid("name", errors)} />
      </Field>

      <Field id="description" label="Descripción" error={errors.description}>
        <Textarea
          id="description"
          name="description"
          rows={4}
          maxLength={2000}
          defaultValue={product?.description}
          className="bg-surface-raised"
          {...invalid("description", errors)}
        />
      </Field>

      <div className="grid gap-5 sm:grid-cols-3">
        <Field id="price" label="Precio (CUP)" error={errors.price}>
          <Input
            id="price"
            name="price"
            inputMode="decimal"
            defaultValue={product?.price.toFixed(2)}
            className="bg-surface-raised font-mono"
            {...invalid("price", errors)}
          />
        </Field>
        <Field id="stock" label="Stock" error={errors.stock}>
          <Input
            id="stock"
            name="stock"
            inputMode="numeric"
            defaultValue={product?.stock ?? 0}
            className="bg-surface-raised font-mono"
            {...invalid("stock", errors)}
          />
        </Field>
        <Field id="productCategoryId" label="Categoría" error={errors.productCategoryId}>
          <Select name="productCategoryId" defaultValue={product ? String(product.category.id) : undefined}>
            <SelectTrigger id="productCategoryId" className="w-full bg-surface-raised" {...invalid("productCategoryId", errors)}>
              <SelectValue placeholder="Elige una" />
            </SelectTrigger>
            <SelectContent>
              {categories.map((category) => (
                <SelectItem key={category.id} value={String(category.id)}>
                  {category.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </Field>
      </div>

      <ImagePicker current={product?.imageUrl ?? null} error={errors.image} />

      <div className="grid gap-5 sm:grid-cols-4">
        <Field id="widthCm" label="Ancho (cm)" error={errors.widthCm}>
          <Input id="widthCm" name="widthCm" inputMode="numeric" defaultValue={product?.widthCm ?? ""} className="bg-surface-raised" {...invalid("widthCm", errors)} />
        </Field>
        <Field id="depthCm" label="Fondo (cm)" error={errors.depthCm}>
          <Input id="depthCm" name="depthCm" inputMode="numeric" defaultValue={product?.depthCm ?? ""} className="bg-surface-raised" {...invalid("depthCm", errors)} />
        </Field>
        <Field id="heightCm" label="Alto (cm)" error={errors.heightCm}>
          <Input id="heightCm" name="heightCm" inputMode="numeric" defaultValue={product?.heightCm ?? ""} className="bg-surface-raised" {...invalid("heightCm", errors)} />
        </Field>
        <Field id="material" label="Material" error={errors.material}>
          <Input id="material" name="material" maxLength={60} defaultValue={product?.material ?? ""} className="bg-surface-raised" {...invalid("material", errors)} />
        </Field>
      </div>

      <div className="flex items-center gap-2">
        <Checkbox id="isActive" name="isActive" defaultChecked={product?.isActive ?? true} />
        <Label htmlFor="isActive" className="text-ink">
          Visible en la tienda
        </Label>
      </div>

      <p role="alert" className="text-sm text-brick empty:hidden">
        {message}
      </p>

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : submitLabel}
        </Button>
      </div>
    </form>
  );
}
