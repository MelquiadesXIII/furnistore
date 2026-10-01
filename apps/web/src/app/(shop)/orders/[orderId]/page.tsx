import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { OrderDetailContainer } from "@/modules/orders/detail/order-detail-container";

export const metadata: Metadata = {
  title: "Detalle del pedido",
  robots: { index: false, follow: false },
};

const ORDER_ID = /^[1-9]\d{0,9}$/;
const MAX_ORDER_ID = 2_147_483_647;

export default async function Page({
  params,
  searchParams,
}: {
  params: Promise<{ orderId: string }>;
  searchParams: Promise<{ placed?: string }>;
}) {
  const [{ orderId }, { placed }] = await Promise.all([params, searchParams]);
  const id = ORDER_ID.test(orderId) ? Number(orderId) : null;

  if (id === null || id > MAX_ORDER_ID) {
    notFound();
  }

  return <OrderDetailContainer id={id} justPlaced={placed === "1"} />;
}
