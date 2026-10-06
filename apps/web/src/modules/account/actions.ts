"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import type { ApiSchemas } from "@/lib/api/contract";
import { updateAccount } from "@/modules/account/api";
import { accountErrorMessage } from "@/modules/account/error-messages";
import { parseAccountForm, type AccountActionResult } from "@/modules/account/account-validation";
import { safeNextPath } from "@/modules/auth/safe-next-path";

export async function saveAccount(
  returnTo: string,
  formData: FormData,
): Promise<AccountActionResult> {
  const { values, fieldErrors } = parseAccountForm(formData);

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
