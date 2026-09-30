"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import type { ApiSchemas } from "@/lib/api/contract";
import { updateAccount } from "@/modules/account/api";
import { accountErrorMessage } from "@/modules/account/error-messages";
import type { AccountField, AccountFieldErrors } from "@/modules/account/types";
import { safeNextPath } from "@/modules/auth/safe-next-path";

export type AccountActionResult =
  | { ok: true }
  | { ok: false; message: string; fieldErrors: AccountFieldErrors };

const PHONE_PATTERN = /^\+?[1-9]\d{6,14}$/;
const PHONE_SEPARATORS = /[\s().-]/g;

const LENGTHS: Record<Exclude<AccountField, "phone">, { min: number; max: number }> = {
  firstName: { min: 2, max: 60 },
  lastName: { min: 2, max: 60 },
  street: { min: 3, max: 200 },
  city: { min: 2, max: 100 },
  province: { min: 2, max: 100 },
  deliveryNotes: { min: 0, max: 300 },
};

function text(formData: FormData, field: AccountField): string {
  const value = formData.get(field);
  return typeof value === "string" ? value.trim() : "";
}

function lengthError(value: string, { min, max }: { min: number; max: number }): string | null {
  if (min > 0 && value.length === 0) return "Este campo es obligatorio.";
  if (value.length < min) return `Escribe al menos ${min} caracteres.`;
  if (value.length > max) return `Usa como máximo ${max} caracteres.`;
  return null;
}

function parse(formData: FormData) {
  const values = {
    firstName: text(formData, "firstName"),
    lastName: text(formData, "lastName"),
    phone: text(formData, "phone").replace(PHONE_SEPARATORS, ""),
    street: text(formData, "street"),
    city: text(formData, "city"),
    province: text(formData, "province"),
    deliveryNotes: text(formData, "deliveryNotes"),
  };

  const fieldErrors: AccountFieldErrors = {};

  for (const field of Object.keys(LENGTHS) as (keyof typeof LENGTHS)[]) {
    const error = lengthError(values[field], LENGTHS[field]);
    if (error) fieldErrors[field] = error;
  }

  if (values.phone.length === 0) {
    fieldErrors.phone = "Este campo es obligatorio.";
  } else if (!PHONE_PATTERN.test(values.phone)) {
    fieldErrors.phone = "Escribe un teléfono válido, de 7 a 15 dígitos.";
  }

  return { values, fieldErrors };
}

export async function saveAccount(
  formData: FormData,
  returnTo: string,
): Promise<AccountActionResult> {
  const { values, fieldErrors } = parse(formData);

  if (Object.keys(fieldErrors).length > 0) {
    return { ok: false, message: "Revisa los campos marcados.", fieldErrors };
  }

  const result = await updateAccount({
    firstName: values.firstName,
    lastName: values.lastName,
    phone: values.phone,
    address: {
      street: values.street,
      city: values.city,
      province: values.province,
      deliveryNotes: values.deliveryNotes || null,
    },
  } satisfies ApiSchemas["UpdateClientRequest"]);

  if (result.ok) {
    revalidatePath("/", "layout");
    return { ok: true };
  }

  if (result.error.kind === "unauthorized") {
    redirect(`/login?next=${encodeURIComponent(safeNextPath(returnTo))}`);
  }

  return { ok: false, message: accountErrorMessage(result.error), fieldErrors: {} };
}
