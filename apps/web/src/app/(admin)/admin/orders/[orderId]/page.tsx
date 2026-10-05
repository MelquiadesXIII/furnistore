import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { AdminOrderContainer } from "@/modules/admin/orders/detail/admin-order-container";
import { parseRouteId } from "@/modules/admin/route-id";

export const metadata: Metadata = { title: "Pedido" };

export default async function Page({ params }: { params: Promise<{ orderId: string }> }) {
  const id = parseRouteId((await params).orderId);
  if (id === null) notFound();

  return <AdminOrderContainer id={id} />;
}
