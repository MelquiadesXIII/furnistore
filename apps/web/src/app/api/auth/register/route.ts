import { NextResponse } from "next/server";
import { clientIpFrom } from "@/lib/api/client-ip";
import { register } from "@/modules/auth/api";
import { toAuthFailure } from "@/modules/auth/error-messages";

export async function POST(request: Request) {
  const { firstName, lastName, emailAddress, password } = await request.json().catch(() => ({}));

  const result = await register(
    { firstName, lastName, emailAddress, password },
    clientIpFrom(request.headers),
  );

  if (!result.ok) {
    return NextResponse.json(
      { error: toAuthFailure(result.error) },
      { status: result.error.status ?? 502 },
    );
  }

  return NextResponse.json({ ok: true, emailSent: result.value.emailSent });
}
