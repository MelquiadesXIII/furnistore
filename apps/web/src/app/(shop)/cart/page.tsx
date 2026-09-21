import type { Metadata } from "next";
import { CartContainer } from "@/modules/cart/list/cart-container";

export const metadata: Metadata = {
  title: "Carrito",
  robots: { index: false, follow: false },
};

export default function Page() {
  return <CartContainer />;
}
