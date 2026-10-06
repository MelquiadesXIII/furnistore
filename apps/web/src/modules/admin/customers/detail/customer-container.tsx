import { notFound } from "next/navigation";
import { Panel } from "@/components/panel";
import { getSessionUser } from "@/lib/session";
import { getAdminCustomer } from "@/modules/admin/customers/api";
import { CustomerView } from "@/modules/admin/customers/detail/customer-view";
import { adminErrorMessage } from "@/modules/admin/error-messages";

export async function CustomerContainer({ id }: { id: number }) {
  const [result, user] = await Promise.all([getAdminCustomer(id), getSessionUser()]);

  if (!result.ok && result.error.kind === "notFound") notFound();
  if (!result.ok) return <Panel>{adminErrorMessage(result.error)}</Panel>;

  return (
    <CustomerView
      customer={result.value}
      isSelf={user?.email.toLowerCase() === result.value.email.toLowerCase()}
    />
  );
}
