import type { Metadata } from "next";
import { NewProductContainer } from "@/modules/admin/products/form/new-product-container";

export const metadata: Metadata = { title: "Nuevo producto" };

export default function Page() {
  return <NewProductContainer />;
}
