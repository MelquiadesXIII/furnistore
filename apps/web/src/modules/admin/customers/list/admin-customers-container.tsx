import type { SearchParams } from "nuqs/server";
import { Panel } from "@/components/panel";
import { getAdminCustomers } from "@/modules/admin/customers/api";
import { CustomersTable } from "@/modules/admin/customers/list/customers-table";
import { loadCustomerSearchParams } from "@/modules/admin/customers/search-params";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { AdminPageHeader } from "@/modules/admin/shell/admin-page-header";
import { SearchFilter, SelectFilter } from "@/modules/admin/table/table-filters";
import { TableFrame } from "@/modules/admin/table/table-frame";
import { TablePagination } from "@/modules/admin/table/table-pagination";

export async function AdminCustomersContainer({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const params = await loadCustomerSearchParams(searchParams);
  const result = await getAdminCustomers(params);

  return (
    <div>
      <AdminPageHeader title="Clientes" description="Cuentas registradas, sus pedidos y su estado." />
      <TableFrame>
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
          <SearchFilter placeholder="Nombre, correo o teléfono" />
          <SelectFilter
            param="filter"
            label="Tipo de cuenta"
            allLabel="Todas las cuentas"
            options={[
              { value: "Admins", label: "Administradores" },
              { value: "LockedOut", label: "Bloqueadas" },
              { value: "Disabled", label: "Desactivadas" },
              { value: "Unconfirmed", label: "Sin confirmar" },
            ]}
          />
        </div>
        {!result.ok ? (
          <Panel>{adminErrorMessage(result.error)}</Panel>
        ) : (
          <>
            <CustomersTable customers={result.value.items} />
            <TablePagination
              page={result.value.page}
              pageSize={result.value.pageSize}
              total={result.value.total}
              totalPages={result.value.totalPages}
            />
          </>
        )}
      </TableFrame>
    </div>
  );
}
