"use server";

import { revalidatePath } from "next/cache";
import { redirect } from "next/navigation";
import type { ApiSchemas } from "@/lib/api/contract";
import { parseAccountForm, type AccountActionResult } from "@/modules/account/account-validation";
import type { AdminActionResult } from "@/modules/admin/action-result";
import {
  changeCustomerAccount,
  updateAdminCustomer,
  type AccountAction,
} from "@/modules/admin/customers/api";
import { adminErrorMessage } from "@/modules/admin/error-messages";

const ACCOUNT_ACTIONS: AccountAction[] = ["unlock", "disable", "enable", "grant-admin", "revoke-admin"];

function isId(value: unknown): value is number {
  return typeof value === "number" && Number.isInteger(value) && value > 0;
}

export async function updateCustomer(
  id: number,
  version: number,
  formData: FormData,
): Promise<AccountActionResult> {
  if (!isId(id) || !Number.isInteger(version) || version < 0) {
    return { ok: false, message: "Cliente no válido.", fieldErrors: {} };
  }

  const { values, fieldErrors } = parseAccountForm(formData);
  if (Object.keys(fieldErrors).length > 0) {
    return { ok: false, message: "Revisa los campos marcados.", fieldErrors };
  }

  const result = await updateAdminCustomer(id, {
    firstName: values.firstName,
    lastName: values.lastName,
    phone: values.phone,
    address: {
      street: values.street,
      city: values.city,
      province: values.province,
      deliveryNotes: values.deliveryNotes || null,
    },
    version,
  } satisfies ApiSchemas["UpdateCustomerRequest"]);

  if (!result.ok && result.error.kind === "unauthorized") redirect(`/login?next=/admin/customers/${id}`);

  revalidatePath("/", "layout");
  return result.ok ? { ok: true } : { ok: false, message: adminErrorMessage(result.error), fieldErrors: {} };
}

export async function changeAccount(id: unknown, action: unknown): Promise<AdminActionResult> {
  if (!isId(id) || !ACCOUNT_ACTIONS.includes(action as AccountAction)) {
    return { ok: false, message: "Acción no válida." };
  }

  const result = await changeCustomerAccount(id, action as AccountAction);

  if (!result.ok && result.error.kind === "unauthorized") redirect(`/login?next=/admin/customers/${id}`);

  revalidatePath("/", "layout");
  return result.ok ? { ok: true } : { ok: false, message: adminErrorMessage(result.error) };
}
