import type { ApiSchemas } from "@/lib/api/contract";

export type ProductField =
  | "name"
  | "description"
  | "price"
  | "stock"
  | "productCategoryId"
  | "imageUrl"
  | "widthCm"
  | "depthCm"
  | "heightCm"
  | "material";

export type ProductFieldErrors = Partial<Record<ProductField, string>>;

type ProductInput = ApiSchemas["CreateProductRequest"];

const PRICE_PATTERN = /^\d{1,7}([.,]\d{1,2})?$/;
const INTEGER_PATTERN = /^\d+$/;
const DIMENSIONS = ["widthCm", "depthCm", "heightCm"] as const;

function text(formData: FormData, field: string): string {
  const value = formData.get(field);
  return typeof value === "string" ? value.trim() : "";
}

function imageHostError(url: string, supabaseUrl: string | undefined): string | null {
  let parsed: URL;
  try {
    parsed = new URL(url);
  } catch {
    return "Escribe una URL completa, que empiece por https://.";
  }

  const allowedHost = supabaseUrl ? new URL(supabaseUrl).hostname : null;
  if (
    parsed.protocol !== "https:" ||
    parsed.hostname !== allowedHost ||
    !parsed.pathname.startsWith("/storage/v1/object/public/")
  ) {
    return "Usa la URL pública de la imagen en el bucket de Supabase.";
  }

  return url.length > 500 ? "La URL es demasiado larga." : null;
}

export function parseProductForm(
  formData: FormData,
  supabaseUrl: string | undefined,
): { input: ProductInput; fieldErrors: ProductFieldErrors } {
  const fieldErrors: ProductFieldErrors = {};

  const name = text(formData, "name");
  if (name.length < 2 || name.length > 120) fieldErrors.name = "Entre 2 y 120 caracteres.";

  const description = text(formData, "description");
  if (description.length > 2000) fieldErrors.description = "Como máximo 2000 caracteres.";

  const priceText = text(formData, "price");
  const price = Number(priceText.replace(",", "."));
  if (!PRICE_PATTERN.test(priceText) || price < 0.01 || price > 1_000_000) {
    fieldErrors.price = "Un precio entre 0,01 y 1.000.000, con hasta 2 decimales.";
  }

  const stockText = text(formData, "stock");
  const stock = Number(stockText);
  if (!INTEGER_PATTERN.test(stockText) || stock > 1_000_000) {
    fieldErrors.stock = "Un número entero entre 0 y 1.000.000.";
  }

  const categoryText = text(formData, "productCategoryId");
  const productCategoryId = Number(categoryText);
  if (!INTEGER_PATTERN.test(categoryText) || productCategoryId < 1) {
    fieldErrors.productCategoryId = "Elige una categoría.";
  }

  const imageUrl = text(formData, "imageUrl");
  const imageError = imageUrl ? imageHostError(imageUrl, supabaseUrl) : null;
  if (imageError) fieldErrors.imageUrl = imageError;

  const dimensions = Object.fromEntries(
    DIMENSIONS.map((field) => {
      const raw = text(formData, field);
      if (!raw) return [field, null];
      const value = Number(raw);
      if (!INTEGER_PATTERN.test(raw) || value < 1 || value > 1000) {
        fieldErrors[field] = "Entre 1 y 1000 cm.";
      }
      return [field, value];
    }),
  ) as Record<(typeof DIMENSIONS)[number], number | null>;

  const material = text(formData, "material");
  if (material.length > 60) fieldErrors.material = "Como máximo 60 caracteres.";

  return {
    input: {
      name,
      description: description || null,
      price,
      stock,
      productCategoryId,
      imageUrl: imageUrl || null,
      ...dimensions,
      material: material || null,
      isActive: formData.get("isActive") === "on",
    },
    fieldErrors,
  };
}
