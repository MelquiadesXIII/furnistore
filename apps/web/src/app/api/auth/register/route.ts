import { NextResponse } from "next/server";
import { toUserMessage } from "@/lib/errors";
import { register } from "@/modules/auth/api";

export async function POST(request: Request) {
  const { firstName, lastName, emailAddress, password } = await request.json();

  const result = await register({ firstName, lastName, emailAddress, password });

  if (!result.ok) {
    const { error } = result;
    return NextResponse.json(
      { errors: error.messages.length > 0 ? error.messages : [toUserMessage(error)] },
      { status: error.status ?? 502 },
    );
  }

  return NextResponse.json({ ok: true, emailSent: result.value.emailSent });
}
