import { createLoader, parseAsInteger, parseAsStringLiteral, type SearchParams } from "nuqs/server";
import { Panel } from "@/components/panel";
import { getAuditEntries } from "@/modules/admin/audit/api";
import { AuditTable } from "@/modules/admin/audit/audit-table";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { AdminPageHeader } from "@/modules/admin/shell/admin-page-header";
import { SelectFilter } from "@/modules/admin/table/table-filters";
import { TableFrame } from "@/modules/admin/table/table-frame";
import { TablePagination } from "@/modules/admin/table/table-pagination";

const loadParams = createLoader({
  page: parseAsInteger.withDefault(1),
  entity: parseAsStringLiteral(["Order", "Product", "ProductCategory", "Customer", "Report"] as const),
});

export async function AuditContainer({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const { page, entity } = await loadParams(searchParams);
  const result = await getAuditEntries({ page, entityType: entity });

  return (
    <div>
      <AdminPageHeader
        title="Auditoría"
        description="Cada cambio hecho desde el panel, con quién lo hizo y qué cambió. No se puede editar."
      />
      <TableFrame>
        <SelectFilter
          param="entity"
          label="Tipo de registro"
          allLabel="Todo"
          options={[
            { value: "Order", label: "Pedidos" },
            { value: "Product", label: "Productos" },
            { value: "ProductCategory", label: "Categorías" },
            { value: "Customer", label: "Clientes" },
            { value: "Report", label: "Reportes exportados" },
          ]}
        />
        {!result.ok ? (
          <Panel>{adminErrorMessage(result.error)}</Panel>
        ) : (
          <>
            <AuditTable entries={result.value.items} />
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
