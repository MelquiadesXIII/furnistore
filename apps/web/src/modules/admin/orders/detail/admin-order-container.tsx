import { notFound } from "next/navigation";
import { Panel } from "@/components/panel";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { getAdminOrder } from "@/modules/admin/orders/api";
import { AdminOrderView } from "@/modules/admin/orders/detail/admin-order-view";

export async function AdminOrderContainer({ id }: { id: number }) {
  const result = await getAdminOrder(id);

  if (!result.ok && result.error.kind === "notFound") {
    notFound();
  }

  return result.ok ? <AdminOrderView detail={result.value} /> : <Panel>{adminErrorMessage(result.error)}</Panel>;
}
