import type { SearchParams } from "nuqs/server";
import { Panel } from "@/components/panel";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { getAdminOrders, getAdminOrderStats } from "@/modules/admin/orders/api";
import { OrdersTable } from "@/modules/admin/orders/list/orders-table";
import { StatusTabs } from "@/modules/admin/orders/list/status-tabs";
import { loadOrderSearchParams } from "@/modules/admin/orders/search-params";
import { AdminPageHeader } from "@/modules/admin/shell/admin-page-header";
import { SearchFilter } from "@/modules/admin/table/table-filters";
import { TableFrame } from "@/modules/admin/table/table-frame";
import { TablePagination } from "@/modules/admin/table/table-pagination";

export async function AdminOrdersContainer({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const params = await loadOrderSearchParams(searchParams);
  const [orders, stats] = await Promise.all([getAdminOrders(params), getAdminOrderStats()]);

  return (
    <div>
      <AdminPageHeader
        title="Pedidos"
        description="Prepara, envía y confirma la entrega. El cliente solo puede cancelar mientras el pedido está pagado."
      />

      <TableFrame>
        <StatusTabs stats={stats.ok ? stats.value : null} />
        <SearchFilter placeholder="Número de pedido, cliente o correo" />
        {!orders.ok ? (
          <Panel>{adminErrorMessage(orders.error)}</Panel>
        ) : (
          <>
            <OrdersTable orders={orders.value.items} />
            <TablePagination
              page={orders.value.page}
              pageSize={orders.value.pageSize}
              total={orders.value.total}
              totalPages={orders.value.totalPages}
            />
          </>
        )}
      </TableFrame>
    </div>
  );
}
