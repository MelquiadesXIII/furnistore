import { OrderCard } from "@/modules/orders/list/order-card";
import type { Order } from "@/modules/orders/types";

export function OrdersGrill({ orders }: { orders: Order[] }) {
  return (
    <ul className="flex flex-col gap-4">
      {orders.map((order) => (
        <OrderCard key={order.id} order={order} />
      ))}
    </ul>
  );
}
