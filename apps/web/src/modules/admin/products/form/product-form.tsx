"use client";

import { cn } from "cn";
import { useState, useTransition, type FormEvent, type ReactNode } from "react";
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
import type { ProductField, ProductFieldErrors } from "@/modules/admin/products/product-validation";
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
        <Field id="price" label="Precio" error={errors.price}>
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

      <Field
        id="imageUrl"
        label="Imagen (opcional)"
        error={errors.imageUrl}
        hint="URL pública del bucket de imágenes en Supabase."
      >
        <Input
          id="imageUrl"
          name="imageUrl"
          type="url"
          defaultValue={product?.imageUrl ?? ""}
          className="bg-surface-raised"
          {...invalid("imageUrl", errors)}
        />
      </Field>

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
