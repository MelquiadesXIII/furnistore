import type { AccountField, AccountFieldErrors } from "@/modules/account/types";

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

export function parseAccountForm(formData: FormData) {
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
