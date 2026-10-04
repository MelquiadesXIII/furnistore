import { toUserMessage, type AppError } from "@/lib/errors";
import type { AuthFailure } from "@/modules/auth/types";

export const EMAIL_NOT_CONFIRMED = "auth.email_not_confirmed";

const AUTH_MESSAGES: Record<string, string> = {
  "auth.invalid_credentials": "Correo o contraseña incorrectos.",
  [EMAIL_NOT_CONFIRMED]:
    "Confirma tu correo antes de iniciar sesión.",
  "auth.code_invalid": "Código incorrecto. Revisa el correo e intenta de nuevo.",
  "auth.code_expired": "El código venció o no existe. Pide uno nuevo.",
  "auth.code_attempts_exceeded": "Demasiados intentos. Pide un código nuevo.",
  "auth.account_disabled": "Esta cuenta está desactivada. Escríbenos si crees que es un error.",
  "auth.locked_out": "Demasiados intentos fallidos. Espera unos minutos e intenta de nuevo.",
  "auth.email_exists": "Ya existe una cuenta con ese correo. Inicia sesión para confirmarla.",
  "auth.registration_failed": "No pudimos crear la cuenta. Revisa los datos e intenta de nuevo.",
};

export function toAuthFailure(error: AppError): AuthFailure {
  const message = (error.code && AUTH_MESSAGES[error.code]) || toUserMessage(error);
  return { code: error.code ?? null, message };
}
