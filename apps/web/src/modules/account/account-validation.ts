import type { AccountField, AccountFieldErrors } from "@/modules/account/types";

export type AccountActionResult =
  | { ok: true }
  | { ok: false; message: string; fieldErrors: AccountFieldErrors };

const PHONE_PATTERN = /^\+?[1-9]\d{6,14}$/;
const PHONE_SEPARATORS = /[\s().-]/g;
const IDENTITY_CARD_PATTERN = /^\d{11}$/;

const LENGTHS: Record<Exclude<AccountField, "phone" | "identityCard">, { min: number; max: number }> = {
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

function isValidIdentityCard(value: string): boolean {
  if (!IDENTITY_CARD_PATTERN.test(value)) return false;

  const century = value[6] === "9" ? 1800 : value[6] >= "6" ? 2000 : 1900;
  const year = century + Number(value.slice(0, 2));
  const month = Number(value.slice(2, 4));
  const day = Number(value.slice(4, 6));
  const date = new Date(Date.UTC(year, month - 1, day));

  return date.getUTCFullYear() === year && date.getUTCMonth() === month - 1 && date.getUTCDate() === day;
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
    identityCard: text(formData, "identityCard").replace(/\s/g, ""),
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

  if (values.identityCard.length === 0) {
    fieldErrors.identityCard = "Este campo es obligatorio.";
  } else if (!isValidIdentityCard(values.identityCard)) {
    fieldErrors.identityCard = "Escribe los 11 dígitos de tu carnet de identidad.";
  }

  return { values, fieldErrors };
}
