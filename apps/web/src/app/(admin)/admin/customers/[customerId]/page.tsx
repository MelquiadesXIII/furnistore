import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { CustomerContainer } from "@/modules/admin/customers/detail/customer-container";
import { parseRouteId } from "@/modules/admin/route-id";

export const metadata: Metadata = { title: "Cliente" };

export default async function Page({ params }: { params: Promise<{ customerId: string }> }) {
  const id = parseRouteId((await params).customerId);
  if (id === null) notFound();

  return <CustomerContainer id={id} />;
}
