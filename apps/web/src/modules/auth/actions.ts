"use server";

import { headers } from "next/headers";
import { clientIpFrom } from "@/lib/api/client-ip";
import { resendConfirmation, verifyEmail } from "@/modules/auth/api";
import { toAuthFailure } from "@/modules/auth/error-messages";

export type AuthActionResult = { ok: true } | { ok: false; message: string };

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const CODE_PATTERN = /^\d{6}$/;
const INVALID_EMAIL = "Escribe un correo válido.";

function normalizeEmail(email: unknown): string | null {
  const address = typeof email === "string" ? email.trim() : "";
  return address.length <= 256 && EMAIL_PATTERN.test(address) ? address : null;
}

export async function resendConfirmationEmail(email: unknown): Promise<AuthActionResult> {
  const address = normalizeEmail(email);
  if (!address) return { ok: false, message: INVALID_EMAIL };

  const result = await resendConfirmation({ email: address }, clientIpFrom(await headers()));

  return result.ok ? { ok: true } : { ok: false, message: toAuthFailure(result.error).message };
}

export async function verifyEmailCode(email: unknown, code: unknown): Promise<AuthActionResult> {
  const address = normalizeEmail(email);
  if (!address) return { ok: false, message: INVALID_EMAIL };

  const digits = typeof code === "string" ? code.replace(/\s/g, "") : "";
  if (!CODE_PATTERN.test(digits)) return { ok: false, message: "El código tiene 6 dígitos." };

  const result = await verifyEmail({ email: address, code: digits }, clientIpFrom(await headers()));

  return result.ok ? { ok: true } : { ok: false, message: toAuthFailure(result.error).message };
}
