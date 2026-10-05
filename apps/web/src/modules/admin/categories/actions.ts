"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import type { AppError } from "@/lib/errors";
import type { AdminActionResult } from "@/modules/admin/action-result";
import {
  createAdminCategory,
  deleteAdminCategory,
  renameAdminCategory,
} from "@/modules/admin/categories/api";
import { adminErrorMessage } from "@/modules/admin/error-messages";

function validName(name: unknown): string | null {
  const trimmed = typeof name === "string" ? name.trim() : "";
  return trimmed.length >= 2 && trimmed.length <= 60 ? trimmed : null;
}

function isId(value: unknown): value is number {
  return typeof value === "number" && Number.isInteger(value) && value > 0;
}

function settle(error: AppError | null): AdminActionResult {
  if (error?.kind === "unauthorized") redirect("/login?next=/admin/categories");
  revalidatePath("/", "layout");
  return error ? { ok: false, message: adminErrorMessage(error) } : { ok: true };
}

const INVALID_NAME = "El nombre debe tener entre 2 y 60 caracteres.";

export async function createCategory(name: unknown): Promise<AdminActionResult> {
  const valid = validName(name);
  if (!valid) return { ok: false, message: INVALID_NAME };

  const result = await createAdminCategory(valid);
  return settle(result.ok ? null : result.error);
}

export async function renameCategory(id: unknown, name: unknown): Promise<AdminActionResult> {
  const valid = validName(name);
  if (!isId(id)) return { ok: false, message: "Categoría no válida." };
  if (!valid) return { ok: false, message: INVALID_NAME };

  const result = await renameAdminCategory(id, valid);
  return settle(result.ok ? null : result.error);
}

export async function deleteCategory(id: unknown): Promise<AdminActionResult> {
  if (!isId(id)) return { ok: false, message: "Categoría no válida." };

  const result = await deleteAdminCategory(id);
  return settle(result.ok ? null : result.error);
}
