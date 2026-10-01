import { notFound, redirect } from "next/navigation";
import { Panel } from "@/components/panel";
import { getOrder } from "@/modules/orders/api";
import { OrderDetailView } from "@/modules/orders/detail/order-detail-view";
import { orderErrorMessage } from "@/modules/orders/error-messages";

export async function OrderDetailContainer({
  id,
  justPlaced,
}: {
  id: number;
  justPlaced: boolean;
}) {
  const result = await getOrder(id);

  if (!result.ok && result.error.kind === "unauthorized") {
    redirect(`/login?next=/orders/${id}`);
  }

  if (!result.ok && result.error.kind === "notFound") {
    notFound();
  }

  return (
    <div className="mx-auto w-full max-w-5xl flex-1 px-6 py-10">
      {result.ok ? (
        <OrderDetailView order={result.value} justPlaced={justPlaced} />
      ) : (
        <Panel>{orderErrorMessage(result.error)}</Panel>
      )}
    </div>
  );
}
