"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import {
  createAdminProduct,
  deleteAdminProduct,
  updateAdminProduct,
} from "@/modules/admin/products/api";
import { parseProductForm, type ProductFieldErrors } from "@/modules/admin/products/product-validation";

export type ProductFormResult =
  | { ok: true; message: string }
  | { ok: false; message: string; fieldErrors: ProductFieldErrors; stale?: boolean };

const INVALID = "Revisa los campos marcados.";

function isId(value: unknown): value is number {
  return typeof value === "number" && Number.isInteger(value) && value > 0;
}

export async function createProduct(formData: FormData): Promise<ProductFormResult> {
  const { input, fieldErrors } = parseProductForm(formData, process.env.SUPABASE_URL);
  if (Object.keys(fieldErrors).length > 0) return { ok: false, message: INVALID, fieldErrors };

  const result = await createAdminProduct(input);

  if (!result.ok) {
    if (result.error.kind === "unauthorized") redirect("/login?next=/admin/products/new");
    return { ok: false, message: adminErrorMessage(result.error), fieldErrors: {} };
  }

  revalidatePath("/", "layout");
  redirect(`/admin/products/${result.value.id}`);
}

export async function updateProduct(
  id: number,
  version: number,
  formData: FormData,
): Promise<ProductFormResult> {
  if (!isId(id) || !Number.isInteger(version) || version < 0) {
    return { ok: false, message: "Producto no válido.", fieldErrors: {} };
  }

  const { input, fieldErrors } = parseProductForm(formData, process.env.SUPABASE_URL);
  if (Object.keys(fieldErrors).length > 0) return { ok: false, message: INVALID, fieldErrors };

  const result = await updateAdminProduct(id, { ...input, version });

  if (!result.ok) {
    if (result.error.kind === "unauthorized") redirect(`/login?next=/admin/products/${id}`);
    return {
      ok: false,
      message: adminErrorMessage(result.error),
      fieldErrors: {},
      stale: result.error.code === "admin.version_conflict",
    };
  }

  revalidatePath("/", "layout");
  return { ok: true, message: "Cambios guardados." };
}

export async function deleteProduct(id: unknown): Promise<{ ok: false; message: string }> {
  if (!isId(id)) return { ok: false, message: "Producto no válido." };

  const result = await deleteAdminProduct(id);

  if (!result.ok) {
    if (result.error.kind === "unauthorized") redirect(`/login?next=/admin/products/${id}`);
    return { ok: false, message: adminErrorMessage(result.error) };
  }

  revalidatePath("/", "layout");
  redirect("/admin/products");
}
