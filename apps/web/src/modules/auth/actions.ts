"use server";

import { headers } from "next/headers";
import { clientIpFrom } from "@/lib/api/client-ip";
import { resendConfirmation } from "@/modules/auth/api";
import { toAuthFailure } from "@/modules/auth/error-messages";

export type ResendConfirmationResult = { ok: true } | { ok: false; message: string };

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export async function resendConfirmationEmail(email: unknown): Promise<ResendConfirmationResult> {
  const address = typeof email === "string" ? email.trim() : "";

  if (address.length > 256 || !EMAIL_PATTERN.test(address)) {
    return { ok: false, message: "Escribe un correo válido." };
  }

  const result = await resendConfirmation({ email: address }, clientIpFrom(await headers()));

  return result.ok ? { ok: true } : { ok: false, message: toAuthFailure(result.error).message };
}
