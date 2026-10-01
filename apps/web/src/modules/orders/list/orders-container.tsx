import Link from "next/link";
import { redirect } from "next/navigation";
import { Pagination } from "@/components/pagination";
import { Panel } from "@/components/panel";
import { getOrders } from "@/modules/orders/api";
import { orderErrorMessage } from "@/modules/orders/error-messages";
import { OrdersGrill } from "@/modules/orders/list/orders-grill";

const PAGE_SIZE = 10;

function hrefFor(page: number) {
  return page > 1 ? `/orders?page=${page}` : "/orders";
}

export async function OrdersContainer({ page }: { page?: string }) {
  const requestedPage = Number.parseInt(page ?? "1", 10);
  const currentPage = Number.isFinite(requestedPage) ? Math.max(1, requestedPage) : 1;

  const result = await getOrders({ page: currentPage, pageSize: PAGE_SIZE });

  if (!result.ok && result.error.kind === "unauthorized") {
    redirect(`/login?next=${encodeURIComponent(hrefFor(currentPage))}`);
  }

  const totalPages = result.ok ? Math.max(1, result.value.totalPages) : 1;

  return (
    <div className="mx-auto w-full max-w-4xl flex-1 px-6 py-10">
      <h1 className="mb-8 border-b border-hairline pb-6 font-display text-3xl font-semibold tracking-tight text-ink">
        Mis pedidos
      </h1>

      {!result.ok ? (
        <Panel>{orderErrorMessage(result.error)}</Panel>
      ) : result.value.total === 0 ? (
        <Panel>
          Todavía no tienes pedidos.{" "}
          <Link href="/" className="font-medium text-accent hover:underline">
            Explora el catálogo
          </Link>
        </Panel>
      ) : result.value.items.length === 0 ? (
        <Panel>
          No hay pedidos en esta página.{" "}
          <Link href="/orders" className="font-medium text-accent hover:underline">
            Ver tus pedidos
          </Link>
        </Panel>
      ) : (
        <>
          <OrdersGrill orders={result.value.items} />
          {totalPages > 1 && (
            <Pagination
              currentPage={Math.min(currentPage, totalPages)}
              totalPages={totalPages}
              hrefFor={hrefFor}
            />
          )}
        </>
      )}
    </div>
  );
}
