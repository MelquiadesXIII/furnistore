import type { Metadata } from "next";
import { OrdersContainer } from "@/modules/orders/list/orders-container";

export const metadata: Metadata = {
  title: "Mis pedidos",
  robots: { index: false, follow: false },
};

export default async function Page({
  searchParams,
}: {
  searchParams: Promise<{ page?: string }>;
}) {
  const { page } = await searchParams;
  return <OrdersContainer page={page} />;
}
