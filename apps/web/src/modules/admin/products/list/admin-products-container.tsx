import { Plus } from "lucide-react";
import Link from "next/link";
import type { SearchParams } from "nuqs/server";
import { Panel } from "@/components/panel";
import { Button } from "@/components/ui/button";
import { getAdminCategories } from "@/modules/admin/categories/api";
import { adminErrorMessage } from "@/modules/admin/error-messages";
import { getAdminProducts } from "@/modules/admin/products/api";
import { ProductsTable } from "@/modules/admin/products/list/products-table";
import { loadProductSearchParams } from "@/modules/admin/products/search-params";
import { AdminPageHeader } from "@/modules/admin/shell/admin-page-header";
import { SearchFilter, SelectFilter } from "@/modules/admin/table/table-filters";
import { TableFrame } from "@/modules/admin/table/table-frame";
import { TablePagination } from "@/modules/admin/table/table-pagination";

export async function AdminProductsContainer({ searchParams }: { searchParams: Promise<SearchParams> }) {
  const params = await loadProductSearchParams(searchParams);
  const [products, categories] = await Promise.all([getAdminProducts(params), getAdminCategories()]);

  return (
    <div>
      <AdminPageHeader
        title="Productos"
        description="Los productos que ya se vendieron no se borran: se archivan para ocultarlos de la tienda."
        actions={
          <Button asChild>
            <Link href="/admin/products/new">
              <Plus />
              Nuevo producto
            </Link>
          </Button>
        }
      />

      <TableFrame>
        <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center">
          <SearchFilter placeholder="Nombre o material" />
          <SelectFilter
            param="status"
            label="Estado"
            allLabel="Todos los estados"
            options={[
              { value: "Active", label: "Activos" },
              { value: "Archived", label: "Archivados" },
            ]}
          />
          <SelectFilter
            param="categoryId"
            label="Categoría"
            allLabel="Todas las categorías"
            options={
              categories.ok
                ? categories.value.items.map((category) => ({ value: String(category.id), label: category.name }))
                : []
            }
          />
          <SelectFilter
            param="maxStock"
            label="Stock"
            allLabel="Cualquier stock"
            options={[
              { value: "3", label: "3 o menos" },
              { value: "0", label: "Agotados" },
            ]}
          />
        </div>

        {!products.ok ? (
          <Panel>{adminErrorMessage(products.error)}</Panel>
        ) : (
          <>
            <ProductsTable products={products.value.items} />
            <TablePagination
              page={products.value.page}
              pageSize={products.value.pageSize}
              total={products.value.total}
              totalPages={products.value.totalPages}
            />
          </>
        )}
      </TableFrame>
    </div>
  );
}
