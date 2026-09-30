import type { Metadata } from "next";
import { CheckoutContainer } from "@/modules/orders/checkout/checkout-container";

export const metadata: Metadata = {
  title: "Finalizar compra",
  robots: { index: false, follow: false },
};

export default function Page() {
  return <CheckoutContainer />;
}
